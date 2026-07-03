using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 로컬 플레이어 전용 어두운 시야.
/// 시야 밖은 "허공"이 아니라 바닥이 안개로 검게 사라지는 짧은 시야 느낌.
/// </summary>
public class CwslLocalDarkVision : MonoBehaviour
{
    // 시야 밖 = 거의 검정 (카메라 배경과 동일해야 허공처럼 안 보임)
    private static readonly Color FogColor = new(0.01f, 0.012f, 0.018f, 1f);
    private static readonly Color CameraBgColor = new(0.01f, 0.012f, 0.018f, 1f);
    private static readonly Color FloorColor = new(0.14f, 0.16f, 0.15f, 1f);
    private static readonly Color BackdropColor = new(0.02f, 0.025f, 0.03f, 1f);

    // 일반 시야
    private static readonly Color AmbientNormal = new(0.16f, 0.17f, 0.2f, 1f);
    private static readonly Color DirectionalNormal = new(0.45f, 0.5f, 0.6f, 1f);
    private const float DirectionalIntensityNormal = 0.35f;
    private const float SpotIntensityNormal = 1.0f;

    // 시야 없음(빨간 마법사): 더 어둡고, 발밑만 짧게
    private static readonly Color AmbientBlind = new(0.04f, 0.045f, 0.055f, 1f);
    private static readonly Color DirectionalBlind = new(0.2f, 0.22f, 0.28f, 1f);
    private const float DirectionalIntensityBlind = 0.08f;
    private const float SpotIntensityBlind = 0.85f;
    private const float BlindVisionRadius = 5f;

    private static readonly Color PlayerLightColor = new(1f, 0.8f, 0.55f, 1f);

    private const float SpotHeight = 8f;
    private const float OuterAnglePadding = 12f;
    private const float InnerAngleRatio = 0.45f;

    private Light playerLight;
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
        ApplyFog();
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
        ApplyFog();
        ConfigureSpotLight(playerLight);
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

        ApplyFog();
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

        // 맵 끝 너머가 허공으로 보이지 않도록 거대한 어두운 바닥
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

    private void ApplyFog()
    {
        var cameraDistance = EstimateCameraDistanceToPlayer();

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = FogColor;

        if (isBlindVision)
        {
            // 발밑만 짧게 보이고, 바로 검게 사라짐 (허공이 아니라 어두운 바닥)
            RenderSettings.fogStartDistance = cameraDistance + currentRadius * 0.15f;
            RenderSettings.fogEndDistance = cameraDistance + currentRadius * 0.95f;
        }
        else
        {
            RenderSettings.fogStartDistance = cameraDistance + currentRadius * 0.35f;
            RenderSettings.fogEndDistance = cameraDistance + currentRadius * 1.25f;
        }
    }

    private float EstimateCameraDistanceToPlayer()
    {
        var camera = Camera.main;
        if (camera == null)
            return 24f;

        return Vector3.Distance(camera.transform.position, transform.position);
    }

    private void EnsurePlayerLight()
    {
        var oldFill = transform.Find("LocalVisionFillLight");
        if (oldFill != null)
            Destroy(oldFill.gameObject);

        var existing = transform.Find("LocalVisionLight");
        GameObject lightObject;
        if (existing != null)
        {
            lightObject = existing.gameObject;
        }
        else
        {
            lightObject = new GameObject("LocalVisionLight");
            lightObject.transform.SetParent(transform, false);
        }

        // 시야 짧을수록 라이트를 조금 낮춰 원형 시야가 또렷하게
        var height = isBlindVision ? 5.5f : SpotHeight;
        lightObject.transform.localPosition = new Vector3(0f, height, 0f);
        lightObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        playerLight = lightObject.GetComponent<Light>();
        if (playerLight == null)
            playerLight = lightObject.AddComponent<Light>();

        ConfigureSpotLight(playerLight, height);
    }

    private void ConfigureSpotLight(Light light, float height)
    {
        if (light == null)
            return;

        var outerAngle = Mathf.Clamp(
            2f * Mathf.Atan(currentRadius / height) * Mathf.Rad2Deg + OuterAnglePadding,
            35f,
            150f);
        var innerAngle = outerAngle * InnerAngleRatio;
        var intensity = isBlindVision ? SpotIntensityBlind : SpotIntensityNormal;

        light.enabled = true;
        light.type = LightType.Spot;
        light.color = PlayerLightColor;
        light.intensity = intensity;
        light.range = currentRadius * 1.6f + height;
        light.spotAngle = outerAngle;
        light.innerSpotAngle = innerAngle;
        light.shadows = LightShadows.None;
        light.renderMode = LightRenderMode.Auto;
        light.cullingMask = ~0;
    }

    private static void ApplyCameraBackground()
    {
        var camera = Camera.main;
        if (camera == null)
            return;

        // 안개색과 동일 → 시야 밖이 하늘/허공이 아니라 어둠으로 보임
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
            playerLight.enabled = false;

        applied = false;
    }
}
