using UnityEngine;

/// <summary>
/// 농장 흙 타일. 플레이어가 5초간 드릴하면 골드를 지급합니다.
/// </summary>
public class FarmDrillTile : MonoBehaviour
{
    [SerializeField] private float flashDuration = 0.25f;

    private Renderer cachedRenderer;
    private Color baseColor;
    private float flashTimer;
    private float cooldownUntil;
    private bool isBeingDrilled;

    private void Awake()
    {
        cachedRenderer = GetComponent<Renderer>();
        if (cachedRenderer != null && cachedRenderer.material != null)
            baseColor = cachedRenderer.material.color;
    }

    private void Update()
    {
        if (flashTimer <= 0f || cachedRenderer == null)
            return;

        flashTimer -= Time.deltaTime;
        float t = Mathf.Clamp01(flashTimer / flashDuration);
        cachedRenderer.material.color = Color.Lerp(baseColor * 1.45f, baseColor, 1f - t);
    }

    public bool CanDrill() => !isBeingDrilled && Time.time >= cooldownUntil;

    public void BeginDrill()
    {
        isBeingDrilled = true;
    }

    public void EndDrill()
    {
        isBeingDrilled = false;
        SetDrillProgress(0f);
    }

    public void SetDrillProgress(float normalized)
    {
        if (cachedRenderer == null)
            return;

        normalized = Mathf.Clamp01(normalized);
        cachedRenderer.material.color = Color.Lerp(baseColor, baseColor * 1.22f, normalized);
    }

    public long CompleteDrill()
    {
        isBeingDrilled = false;
        SetDrillProgress(0f);

        long reward = FarmGoldEconomy.RollDrillReward();
        if (GameManager.Instance != null)
            GameManager.Instance.AddMoney(reward);

        FarmGoldAudio.PlayCoin(transform.position);
        FarmDrillVfx.PlayGoldBurst(transform.position);
        flashTimer = flashDuration;
        cooldownUntil = Time.time + FarmGoldEconomy.TileCooldownSeconds;
        return reward;
    }
}
