using UnityEngine;

/// <summary>
/// 질주자 벽 충돌 스턴 — Epic Toon FX 폭발 + 머리 위 별.
/// </summary>
public class CwslPlayerRammerStunVisual : MonoBehaviour
{
    private Transform starsAnchor;
    private GameObject starsInstance;
    private CwslPlayerStun playerStun;
    private CwslPlayerHealth playerHealth;
    private bool rammerStunFxActive;

    private void Awake()
    {
        playerStun = GetComponentInParent<CwslPlayerStun>();
        playerHealth = GetComponentInParent<CwslPlayerHealth>();
        EnsureStarsAnchor();
    }

    private void OnEnable()
    {
        if (playerStun != null)
            playerStun.OnStunStateChanged += HandleStunStateChanged;

        SyncWithCurrentStunState();
    }

    private void OnDisable()
    {
        if (playerStun != null)
            playerStun.OnStunStateChanged -= HandleStunStateChanged;

        EndStunVisual();
    }

    public void PlayStunVfx(Vector3 worldPosition)
    {
        rammerStunFxActive = true;
        EnsureStarsAnchor();
        CwslVfxSpawner.SpawnRammerStunExplosion(worldPosition);
        EnsureStars();
    }

    public void OnStunEnded()
    {
        EndStunVisual();
    }

    private void HandleStunStateChanged(bool stunned)
    {
        if (stunned)
        {
            rammerStunFxActive = true;
            EnsureStarsAnchor();
            EnsureStars();
            return;
        }

        EndStunVisual();
    }

    private void SyncWithCurrentStunState()
    {
        if (playerHealth != null && !playerHealth.IsAlive)
        {
            EndStunVisual();
            return;
        }

        if (playerStun != null && playerStun.IsStunned)
        {
            rammerStunFxActive = true;
            EnsureStarsAnchor();
            EnsureStars();
            return;
        }

        EndStunVisual();
    }

    private void EndStunVisual()
    {
        rammerStunFxActive = false;
        ClearStars();
    }

    private void EnsureStarsAnchor()
    {
        if (starsAnchor != null)
            return;

        var headPivot = transform.Find("HorseRoot/RiderPivot/HeadPivot");
        if (headPivot == null)
            return;

        starsAnchor = new GameObject("RammerStunStarsAnchor").transform;
        starsAnchor.SetParent(headPivot, false);
        starsAnchor.localPosition = new Vector3(0f, 0.42f, 0f);
    }

    private void EnsureStars()
    {
        if (!rammerStunFxActive || starsInstance != null)
            return;

        EnsureStarsAnchor();
        if (starsAnchor == null)
            return;

        starsInstance = CwslVfxSpawner.AttachRammerStunStars(starsAnchor);
    }

    private void ClearStars()
    {
        if (starsInstance == null)
            return;

        Destroy(starsInstance);
        starsInstance = null;
    }

    private void OnDestroy()
    {
        ClearStars();
    }
}
