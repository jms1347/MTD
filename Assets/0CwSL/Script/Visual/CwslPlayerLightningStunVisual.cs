using UnityEngine;

/// <summary>
/// 라이트닝 미사일 스턴 — Epic Toon FX 폭발·낙뢰 + 플레이어 주변 번개 오브.
/// </summary>
public class CwslPlayerLightningStunVisual : MonoBehaviour
{
    private Transform auraAnchor;
    private GameObject auraInstance;
    private CwslPlayerStun playerStun;
    private CwslPlayerHealth playerHealth;
    private bool lightningStunActive;
    private bool wasStunned;

    private void Awake()
    {
        playerStun = GetComponent<CwslPlayerStun>();
        playerHealth = GetComponent<CwslPlayerHealth>();

        auraAnchor = new GameObject("LightningStunAuraAnchor").transform;
        auraAnchor.SetParent(transform, false);
        auraAnchor.localPosition = new Vector3(0f, 0.95f, 0f);
    }

    private void Update()
    {
        if (playerStun == null)
            return;

        if (playerHealth != null && !playerHealth.IsAlive)
        {
            ClearAura();
            lightningStunActive = false;
            wasStunned = false;
            return;
        }

        var stunned = playerStun.IsStunned;
        if (lightningStunActive && stunned && auraInstance == null)
            EnsureAura();

        if ((!stunned || !lightningStunActive) && wasStunned)
        {
            ClearAura();
            lightningStunActive = false;
        }

        wasStunned = stunned;
    }

    public void PlayLightningStunVfx(Vector3 worldPosition)
    {
        lightningStunActive = true;
        CwslVfxSpawner.SpawnLightningStunExplosion(worldPosition);
        CwslVfxSpawner.SpawnLightningStunStrike(worldPosition);
        EnsureAura();
    }

    private void EnsureAura()
    {
        if (auraInstance != null || auraAnchor == null)
            return;

        auraInstance = CwslVfxSpawner.AttachLightningStunAura(auraAnchor);
    }

    private void ClearAura()
    {
        if (auraInstance == null)
            return;

        Destroy(auraInstance);
        auraInstance = null;
    }

    private void OnDestroy()
    {
        ClearAura();
    }
}
