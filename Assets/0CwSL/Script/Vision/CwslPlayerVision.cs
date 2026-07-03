using Unity.Netcode;
using UnityEngine;

public class CwslPlayerVision : NetworkBehaviour
{
    public static CwslPlayerVision Local { get; private set; }

    private CwslPlayerCharacter playerCharacter;
    private LineRenderer visionRing;
    private float visionRadius = 14f;
    private bool localReady;

    public float VisionRadius => visionRadius;
    public Vector3 VisionOrigin => transform.position;

    public override void OnNetworkSpawn()
    {
        playerCharacter = GetComponent<CwslPlayerCharacter>();
        if (playerCharacter != null)
            playerCharacter.OnCharacterChanged += HandleCharacterChanged;

        RefreshVisionRadius();
        TryInitializeLocal();
    }

    private void Start()
    {
        TryInitializeLocal();
    }

    public override void OnNetworkDespawn()
    {
        if (playerCharacter != null)
            playerCharacter.OnCharacterChanged -= HandleCharacterChanged;

        if (Local == this)
            Local = null;
        localReady = false;
    }

    public static bool IsInLocalVision(Vector3 worldPosition)
    {
        if (Local == null)
            return true;

        var flat = worldPosition - Local.VisionOrigin;
        flat.y = 0f;
        return flat.sqrMagnitude <= Local.VisionRadius * Local.VisionRadius;
    }

    private void TryInitializeLocal()
    {
        if (localReady || !IsSpawned || !IsOwner)
            return;

        localReady = true;
        Local = this;
        EnsureVisionRing();
        EnsureVisionSystem();
        ApplyRingRadius();
    }

    private void HandleCharacterChanged(CwslCharacterId characterId)
    {
        RefreshVisionRadius();
        if (IsOwner)
            ApplyRingRadius();
    }

    private void RefreshVisionRadius()
    {
        var characterId = playerCharacter != null ? playerCharacter.CharacterId : CwslCharacterId.Tank;
        visionRadius = CwslCharacterCatalog.GetVisionRadius(characterId);
    }

    private void LateUpdate()
    {
        if (!IsOwner || visionRing == null)
            return;

        var origin = transform.position;
        origin.y = 0.08f;
        visionRing.transform.position = origin;
    }

    private void EnsureVisionSystem()
    {
        if (GetComponent<CwslLocalVisionSystem>() == null)
            gameObject.AddComponent<CwslLocalVisionSystem>();
    }

    private void EnsureVisionRing()
    {
        if (visionRing != null)
            return;

        // 기존에 남아 있을 수 있는 큰 시야 원판 제거
        var oldDisc = transform.Find("VisionDisc");
        if (oldDisc != null)
            Destroy(oldDisc.gameObject);

        var existingRing = transform.Find("VisionRing");
        if (existingRing != null)
            Destroy(existingRing.gameObject);

        var ringObject = new GameObject("VisionRing");
        ringObject.transform.SetParent(transform, false);
        ringObject.transform.localPosition = Vector3.zero;

        visionRing = ringObject.AddComponent<LineRenderer>();
        visionRing.useWorldSpace = false;
        visionRing.loop = true;
        visionRing.widthMultiplier = 0.08f;
        visionRing.positionCount = 72;
        visionRing.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        visionRing.receiveShadows = false;
        // 얇은 점선 느낌의 시야 경계 (방패와 구분)
        visionRing.material = CreateRingMaterial(new Color(0.55f, 0.9f, 1f, 0.45f));
    }

    private void ApplyRingRadius()
    {
        if (visionRing == null)
            return;

        const int segments = 72;
        visionRing.positionCount = segments;
        for (var i = 0; i < segments; i++)
        {
            var angle = i / (float)segments * Mathf.PI * 2f;
            visionRing.SetPosition(i, new Vector3(
                Mathf.Cos(angle) * visionRadius,
                0.08f,
                Mathf.Sin(angle) * visionRadius));
        }
    }

    private static Material CreateRingMaterial(Color color)
    {
        var material = CwslMaterialUtil.CreateColored(color);
        material.SetFloat("_Mode", 3f);
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 3000;
        material.color = color;
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        return material;
    }
}
