using UnityEngine;

/// <summary>
/// Health 피격 시 머리 위 데미지 팝업을 표시합니다.
/// </summary>
[RequireComponent(typeof(Health))]
public class HealthDamagePopupBridge : MonoBehaviour
{
    [SerializeField] private float headPadding = 0.22f;

    private Health health;

    private void Awake()
    {
        health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        if (health != null)
            health.OnDamagedInfo += HandleDamaged;
    }

    private void OnDisable()
    {
        if (health != null)
            health.OnDamagedInfo -= HandleDamaged;
    }

    private void HandleDamaged(DamageInfo info)
    {
        if (info.amount <= 0f)
            return;

        GetComponent<HealthBarUI>()?.NotifyDamaged();
        CombatDamagePopupPool.Play(ResolveWorldAnchor(), info.amount, info.element);
    }

    private Vector3 ResolveWorldAnchor()
    {
        float headOffset = CompareTag("Nexus")
            ? 3.85f
            : MonsterGroundPlacement.ResolveHeadOffset(transform, 0.82f);
        return transform.TransformPoint(new Vector3(0f, headOffset + headPadding, 0f));
    }
}
