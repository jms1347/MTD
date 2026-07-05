using Unity.Netcode;
using UnityEngine;

public class CwslPlayerVision : NetworkBehaviour
{
    public static CwslPlayerVision Local { get; private set; }

    private CwslPlayerCharacter playerCharacter;
    private CwslLocalDarkVision darkVision;
    private CwslPlayerVisionDebuff visionDebuff;
    private float visionRadius = 14f;
    private bool localReady;
    private CwslPlayerVisionScry activeScry;

    public float VisionRadius => visionRadius;
    public bool HasActiveScry => activeScry.IsActive;

    /// <summary>시야 0 캐릭터도 발밑 짧은 반경은 보이게 하는 실제 판정/연출 반경.</summary>
    public float EffectiveVisionRadius =>
        IsBlindVision ? CwslGameConstants.BlindVisionRadius : ResolveEffectiveVisionRadius();

    public bool IsBlindVision => visionRadius <= 0.01f || IsForcedBlind;

    public bool IsForcedBlind => visionDebuff != null && visionDebuff.IsForcedBlind;

    public Vector3 VisionOrigin => transform.position;

    public override void OnNetworkSpawn()
    {
        playerCharacter = GetComponent<CwslPlayerCharacter>();
        visionDebuff = GetComponent<CwslPlayerVisionDebuff>();
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

        if (Local.TryGetScryVisibility(worldPosition, isProjectile: false) > 0.01f)
            return true;

        var flat = worldPosition - Local.VisionOrigin;
        flat.y = 0f;
        var radius = Local.EffectiveVisionRadius;
        return flat.sqrMagnitude <= radius * radius;
    }

    public void RevealMeteorScry(Vector3 worldCenter)
    {
        if (!IsOwner || !IsBlindVision)
            return;

        activeScry = CwslPlayerVisionScry.Create(
            worldCenter,
            CwslGameConstants.RedMageMeteorScryRadius,
            CwslGameConstants.RedMageMeteorScryDuration);
    }

    public float TryGetScryVisibility(Vector3 worldPosition, bool isProjectile)
    {
        return activeScry.IsActive ? activeScry.EvaluateVisibility(worldPosition, isProjectile) : 0f;
    }

    public bool TryGetActiveScry(out Vector3 center, out float radius)
    {
        if (!activeScry.IsActive)
        {
            center = default;
            radius = 0f;
            return false;
        }

        center = activeScry.Center;
        radius = activeScry.Radius;
        return true;
    }

    private void TryInitializeLocal()
    {
        if (localReady || !IsSpawned || !IsOwner)
            return;

        localReady = true;
        Local = this;
        EnsureDarkVision();
        CleanupHardVisionMasks();
        EnsureVisionSystem();
        EnsureFogZoneEffect();
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
        if (darkVision != null)
            darkVision.RefreshRadius(visionRadius, GetLighthouseVisionBonus());
    }

    private float ResolveEffectiveVisionRadius()
    {
        return visionRadius + GetLighthouseVisionBonus();
    }

    private float GetLighthouseVisionBonus()
    {
        if (IsBlindVision)
            return 0f;

        return CwslArenaGimmickSystem.GetLighthouseVisionBonus(transform.position);
    }

    private void Update()
    {
        if (!IsOwner || !localReady || darkVision == null)
            return;

        darkVision.RefreshRadius(visionRadius, GetLighthouseVisionBonus());
    }

    private void EnsureDarkVision()
    {
        darkVision = GetComponent<CwslLocalDarkVision>();
        if (darkVision == null)
            darkVision = gameObject.AddComponent<CwslLocalDarkVision>();
        darkVision.Activate(visionRadius, GetLighthouseVisionBonus());
    }

    private void EnsureVisionSystem()
    {
        if (GetComponent<CwslLocalVisionSystem>() == null)
            gameObject.AddComponent<CwslLocalVisionSystem>();
    }

    private void EnsureFogZoneEffect()
    {
        if (GetComponent<CwslLocalFogZoneEffect>() == null)
            gameObject.AddComponent<CwslLocalFogZoneEffect>();
    }

    private void CleanupHardVisionMasks()
    {
        // 칼질 경계 링/원판 제거 (안개 + smoothstep 페이드만 사용)
        var oldDisc = transform.Find("VisionDisc");
        if (oldDisc != null)
            Destroy(oldDisc.gameObject);

        var existingRing = transform.Find("VisionRing");
        if (existingRing != null)
            Destroy(existingRing.gameObject);
    }
}
