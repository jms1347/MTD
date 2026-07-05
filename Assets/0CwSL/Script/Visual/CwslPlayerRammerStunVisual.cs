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
    private bool wasStunned;

    private void Awake()
    {
        playerStun = GetComponentInParent<CwslPlayerStun>();
        playerHealth = GetComponentInParent<CwslPlayerHealth>();

        var headPivot = transform.Find("HorseRoot/RiderPivot/HeadPivot");
        if (headPivot == null)
            return;

        starsAnchor = new GameObject("RammerStunStarsAnchor").transform;
        starsAnchor.SetParent(headPivot, false);
        starsAnchor.localPosition = new Vector3(0f, 0.42f, 0f);
    }

    private void Start()
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

    private void Update()
    {
        if (playerStun == null || starsAnchor == null)
            return;

        if (playerHealth != null && !playerHealth.IsAlive)
        {
            ClearStars();
            wasStunned = false;
            return;
        }

        var stunned = playerStun.IsStunned;
        if (stunned && !wasStunned)
            EnsureStars();

        if (!stunned && wasStunned)
            ClearStars();

        wasStunned = stunned;
    }

    public void PlayStunVfx(Vector3 worldPosition)
    {
        CwslVfxSpawner.SpawnRammerStunExplosion(worldPosition);
        EnsureStars();
    }

    private void EnsureStars()
    {
        if (starsInstance != null || starsAnchor == null)
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
