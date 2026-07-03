using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 로컬 플레이어 전용 어두운 시야.
/// 1) 스크린 스페이스 비네팅 — 쿼터뷰에서도 위쪽이 잘리지 않음
/// 2) 카메라와 같은 각도의 Soft Spot Light — 월드 은은한 조명
/// </summary>
public class CwslLocalDarkVision : MonoBehaviour
{
    private static readonly Color FogColor = new(0.01f, 0.012f, 0.018f, 1f);
    private static readonly Color CameraBgColor = new(0.01f, 0.012f, 0.018f, 1f);
    private static readonly Color FloorColor = new(0.16f, 0.18f, 0.17f, 1f);
    private static readonly Color BackdropColor = new(0.02f, 0.025f, 0.03f, 1f);

    private static readonly Color AmbientNormal = new(0.18f, 0.19f, 0.22f, 1f);
    private static readonly Color DirectionalNormal = new(0.4f, 0.45f, 0.55f, 1f);
    private const float DirectionalIntensityNormal = 0.28f;

    private static readonly Color AmbientBlind = new(0.05f, 0.055f, 0.065f, 1f);
    private static readonly Color DirectionalBlind = new(0.18f, 0.2f, 0.25f, 1f);
    private const float DirectionalIntensityBlind = 0.06f;

    // 웜화이트, 은은하게
    private static readonly Color PlayerLightColor = new(1f, 0.82f, 0.58f, 1f);
    private const float SpotIntensityNormal = 1.0f;
    private const float SpotIntensityBlind = 0.75f;
    private const float BlindVisionRadius = 2.8f;

    private Light playerLight;
    private CwslScreenSpaceVision screenVision;
    private bool applied;
    private bool floorPrepared;
    private float currentRadius = 14f;
    private bool isBlindVision;

    private bool hadFog;
    private FogMode previousFogMode;
    private Color previousFogColor;
    private float previousFogStart;
    private float previousFogEnd;
    private float previousFogDensity;
    private Color previousAmbient;
    private AmbientMode previousAmbientMode;
    private readonly System.Collections.Generic.List<LightState> directionalStates = new();

    private struct LightState
    {
        public Light Light;
        public float Intensity;
        public Color Color;
    }

    public float EffectiveVisionRadius => currentRadius;

    public void Activate(float visionRadius)
    {
        ApplyRadius(visionRadius);
        if (!applied)
        {
            CacheEnvironment();
            applied = true;
        }

        ApplyNightEnvironment();
        PrepareFloor();
        ApplyFarFogOnly();
        EnsureScreenVision();
        EnsurePlayerLight();
        ApplyCameraBackground();
    }

    public void RefreshRadius(float visionRadius)
    {
        ApplyRadius(visionRadius);
        if (!applied)
        {
            Activate(visionRadius);
            return;
        }

        ApplyNightEnvironment();
        ApplyFarFogOnly();
        if (screenVision != null)
            screenVision.SetVisionRadius(visionRadius);
        ConfigureCameraAlignedLight(playerLight);
    }

    private void ApplyRadius(float visionRadius)
    {
        isBlindVision = visionRadius <= 0.01f;
        currentRadius = isBlindVision ? BlindVisionRadius : Mathf.Max(8f, visionRadius);
    }

    private void LateUpdate()
    {
        if (!applied)
            return;

        // 매 프레임 카메라 각도에 맞춰 라이트 정렬
        ConfigureCameraAlignedLight(playerLight);
        ApplyFarFogOnly();
    }

    private void OnDisable()
    {
        RestoreEnvironment();
    }

    private void OnDestroy()
    {
        RestoreEnvironment();
    }

    private void CacheEnvironment()
    {
        hadFog = RenderSettings.fog;
        previousFogMode = RenderSettings.fogMode;
        previousFogColor = RenderSettings.fogColor;
        previousFogStart = RenderSettings.fogStartDistance;
        previousFogEnd = RenderSettings.fogEndDistance;
        previousFogDensity = RenderSettings.fogDensity;
        previousAmbient = RenderSettings.ambientLight;
        previousAmbientMode = RenderSettings.ambientMode;

        directionalStates.Clear();
        foreach (var light in FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (light == null || light.type != LightType.Directional)
                continue;

            directionalStates.Add(new LightState
            {
                Light = light,
                Intensity = light.intensity,
                Color = light.color
            });
        }
    }

    private void ApplyNightEnvironment()
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = isBlindVision ? AmbientBlind : AmbientNormal;

        var dirIntensity = isBlindVision ? DirectionalIntensityBlind : DirectionalIntensityNormal;
        var dirColor = isBlindVision ? DirectionalBlind : DirectionalNormal;

        foreach (var state in directionalStates)
        {
            if (state.Light == null)
                continue;

            state.Light.intensity = dirIntensity;
            state.Light.color = dirColor;
        }
    }

    private void PrepareFloor()
    {
        if (floorPrepared)
            return;

        floorPrepared = true;

        var floor = GameObject.Find("ArenaPlane");
        if (floor != null)
        {
            var renderer = floor.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material = CwslMaterialUtil.CreateMatteColored(FloorColor);
        }

        if (GameObject.Find("VisionDarkBackdrop") != null)
            return;

        var backdrop = GameObject.CreatePrimitive(PrimitiveType.Plane);
        backdrop.name = "VisionDarkBackdrop";
        backdrop.transform.position = new Vector3(0f, -0.08f, 0f);
        backdrop.transform.localScale = new Vector3(250f, 1f, 250f);

        var collider = backdrop.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);

        var backdropRenderer = backdrop.GetComponent<Renderer>();
        if (backdropRenderer != null)
            backdropRenderer.material = CwslMaterialUtil.CreateMatteColored(BackdropColor);
    }

    /// <summary>
    /// 시야 가림은 스크린 스페이스가 담당.
    /// 월드 Fog는 아주 먼 거리만 살짝 어둡게 (위쪽 잘림 방지).
    /// </summary>
    private void ApplyFarFogOnly()
    {
        var camera = Camera.main;
        var cameraDistance = camera != null
            ? Vector3.Distance(camera.transform.position, transform.position)
            : 24f;

        // 시야 원 전체가 안개 시작보다 앞에 오도록 충분히 멀리
        var maxVisionCamDist = EstimateMaxCameraDistanceInVisionCircle(currentRadius);

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = FogColor;
        RenderSettings.fogStartDistance = maxVisionCamDist + currentRadius * 0.8f;
        RenderSettings.fogEndDistance = maxVisionCamDist + currentRadius * 3.5f + 10f;
    }

    private float EstimateMaxCameraDistanceInVisionCircle(float visionRadius)
    {
        var camera = Camera.main;
        if (camera == null)
            return 24f + visionRadius;

        var camPos = camera.transform.position;
        var playerPos = transform.position;
        playerPos.y = 0f;

        var toPlayer = playerPos - camPos;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude < 0.0001f)
            return Vector3.Distance(camPos, playerPos) + visionRadius;

        var farPoint = playerPos + toPlayer.normalized * visionRadius;
        farPoint.y = 0f;
        return Vector3.Distance(camPos, farPoint);
    }

    private void EnsureScreenVision()
    {
        screenVision = GetComponent<CwslScreenSpaceVision>();
        if (screenVision == null)
            screenVision = gameObject.AddComponent<CwslScreenSpaceVision>();

        screenVision.Activate(transform, isBlindVision ? 0f : currentRadius);
    }

    private void EnsurePlayerLight()
    {
        var oldFill = transform.Find("LocalVisionFillLight");
        if (oldFill != null)
            Destroy(oldFill.gameObject);

        if (playerLight == null)
        {
            var existing = transform.Find("LocalVisionLight");
            GameObject lightObject;
            if (existing != null)
            {
                lightObject = existing.gameObject;
            }
            else
            {
                // 월드 공간에서 카메라와 정렬되므로 부모 없이 둠
                lightObject = new GameObject("LocalVisionLight");
            }

            playerLight = lightObject.GetComponent<Light>();
            if (playerLight == null)
                playerLight = lightObject.AddComponent<Light>();
        }

        ConfigureCameraAlignedLight(playerLight);
    }

    /// <summary>
    /// 카메라와 같은 각도로 Spot Light를 쏴서, 화면에서 시야가 원형으로 보이게 함.
    /// (수직 조명은 쿼터뷰에서 위쪽이 잘림)
    /// </summary>
    private void ConfigureCameraAlignedLight(Light light)
    {
        if (light == null)
            return;

        var camera = Camera.main;
        var intensity = isBlindVision ? SpotIntensityBlind : SpotIntensityNormal;

        light.enabled = true;
        light.type = LightType.Spot;
        light.color = PlayerLightColor;
        light.intensity = intensity;
        light.shadows = LightShadows.None;
        light.renderMode = LightRenderMode.Auto;
        light.cullingMask = ~0;

        if (camera == null)
        {
            light.transform.localPosition = new Vector3(0f, 8f, 0f);
            light.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            light.range = currentRadius * 2f;
            light.spotAngle = 80f;
            light.innerSpotAngle = 30f;
            return;
        }

        // 카메라와 평행한 방향으로, 플레이어 뒤/위에서 비춤
        var lightDistance = isBlindVision ? 6f : 10f;
        var worldPos = transform.position - camera.transform.forward * lightDistance;
        light.transform.position = worldPos;
        light.transform.rotation = camera.transform.rotation;

        // 시야 반경이 화면에 원형으로 담기도록 각도 여유
        light.range = lightDistance + currentRadius * 2.2f;
        light.spotAngle = isBlindVision ? 42f : 58f;
        light.innerSpotAngle = isBlindVision ? 14f : 22f;
    }

    private static void ApplyCameraBackground()
    {
        var camera = Camera.main;
        if (camera == null)
            return;

        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = CameraBgColor;
    }

    private void RestoreEnvironment()
    {
        if (!applied)
            return;

        RenderSettings.fog = hadFog;
        RenderSettings.fogMode = previousFogMode;
        RenderSettings.fogColor = previousFogColor;
        RenderSettings.fogStartDistance = previousFogStart;
        RenderSettings.fogEndDistance = previousFogEnd;
        RenderSettings.fogDensity = previousFogDensity;
        RenderSettings.ambientLight = previousAmbient;
        RenderSettings.ambientMode = previousAmbientMode;

        foreach (var state in directionalStates)
        {
            if (state.Light == null)
                continue;

            state.Light.intensity = state.Intensity;
            state.Light.color = state.Color;
        }

        if (playerLight != null)
        {
            playerLight.enabled = false;
            Destroy(playerLight.gameObject);
            playerLight = null;
        }

        if (screenVision != null)
            screenVision.Deactivate();

        applied = false;
    }
}
