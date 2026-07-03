using Unity.Netcode;
using UnityEngine;

public class CwslPlayerVision : NetworkBehaviour
{
    public static CwslPlayerVision Local { get; private set; }

    private CwslPlayerCharacter playerCharacter;
    private CwslLocalDarkVision darkVision;
    private LineRenderer visionRing;
    private float visionRadius = 14f;
    private bool localReady;

    public float VisionRadius => visionRadius;

    /// <summary>시야 0 캐릭터도 발밑 짧은 반경은 보이게 하는 실제 판정/연출 반경.</summary>
    public float EffectiveVisionRadius =>
        darkVision != null ? darkVision.EffectiveVisionRadius :
        visionRadius <= 0.01f ? 5f : visionRadius;

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
        var radius = Local.EffectiveVisionRadius;
        return flat.sqrMagnitude <= radius * radius;
    }

    private void TryInitializeLocal()
    {
        if (localReady || !IsSpawned || !IsOwner)
            return;

        localReady = true;
        Local = this;
        EnsureDarkVision();
        EnsureVisionRing();
        EnsureVisionSystem();
        ApplyVisionRadius();
    }

    private void HandleCharacterChanged(CwslCharacterId characterId)
    {
        RefreshVisionRadius();
        if (IsOwner)
            ApplyVisionRadius();
    }

    private void RefreshVisionRadius()
    {
        var characterId = playerCharacter != null ? playerCharacter.CharacterId : CwslCharacterId.Tank;
        visionRadius = CwslCharacterCatalog.GetVisionRadius(characterId);
    }

    private void ApplyVisionRadius()
    {
        ApplyRingRadius();
        if (darkVision != null)
            darkVision.RefreshRadius(visionRadius);
    }

    private void LateUpdate()
    {
        if (!IsOwner || visionRing == null)
            return;

        var origin = transform.position;
        origin.y = 0.08f;
        visionRing.transform.position = origin;
    }

    private void EnsureDarkVision()
    {
        darkVision = GetComponent<CwslLocalDarkVision>();
        if (darkVision == null)
            darkVision = gameObject.AddComponent<CwslLocalDarkVision>();
        darkVision.Activate(visionRadius);
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
        visionRing.widthMultiplier = 0.1f;
        visionRing.positionCount = 72;
        visionRing.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        visionRing.receiveShadows = false;
        visionRing.material = CreateRingMaterial(new Color(1f, 0.85f, 0.45f, 0.55f));
    }

    private void ApplyRingRadius()
    {
        if (visionRing == null)
            return;

        // 시야 0이면 링 숨김 (본인만 보이는 캐릭터)
        var showRing = visionRadius > 0.5f;
        visionRing.enabled = showRing;
        if (!showRing)
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
