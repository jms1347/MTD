using Unity.Netcode;
using UnityEngine;

public abstract class CwslMonsterBase : NetworkBehaviour
{
    [SerializeField] protected float moveSpeed = 3.5f;

    protected CwslMonsterHealth health;
    protected NetworkObject currentTarget;
    protected float targetRefreshTimer;

    public CwslMonsterType MonsterType { get; protected set; }

    public virtual void Initialize(CwslMonsterType type)
    {
        MonsterType = type;
        health = GetComponent<CwslMonsterHealth>();
        health?.Configure(type);
        EnsureMeleeLungeVisual();
        EnsureThreatLight();
        CwslMonsterMaterialFix.Refresh(transform, type);
    }

    private void EnsureThreatLight()
    {
        if (MonsterType == CwslMonsterType.Suicide)
        {
            CwslThreatLight.Ensure(transform, new Color(1f, 0.2f, 0.05f), 5.5f, 3.2f, new Vector3(0f, 0.8f, 0f));
            return;
        }

        if (MonsterType == CwslMonsterType.Ranged)
        {
            CwslThreatLight.Ensure(transform, new Color(0.7f, 0.25f, 1f), 3.2f, 1.4f, new Vector3(0f, 1.0f, 0f));
        }
    }

    private void EnsureMeleeLungeVisual()
    {
        if (MonsterType != CwslMonsterType.Melee)
            return;

        var visual = transform.Find("Visual");
        if (visual != null && visual.GetComponent<CwslMeleeLungeVisual>() == null)
            visual.gameObject.AddComponent<CwslMeleeLungeVisual>();
    }

    protected virtual void Update()
    {
        if (!IsServer || !IsSpawned)
            return;

        if (health == null || !health.IsAlive)
            return;

        targetRefreshTimer -= Time.deltaTime;
        if (targetRefreshTimer <= 0f || !IsValidTarget(currentTarget))
        {
            targetRefreshTimer = 0.35f;
            RefreshTarget();
        }

        if (!IsValidTarget(currentTarget))
            return;

        TickServerAI();
    }

    protected abstract void TickServerAI();

    protected void RefreshTarget()
    {
        if (CwslTargetQuery.TryGetNearestLivingPlayer(transform.position, out var target, out _))
            currentTarget = target;
        else
            currentTarget = null;
    }

    protected static bool IsValidTarget(NetworkObject target)
    {
        if (target == null || !target.IsSpawned)
            return false;

        var playerHealth = target.GetComponent<CwslPlayerHealth>();
        return playerHealth == null || playerHealth.IsAlive;
    }

    protected void MoveToward(Vector3 worldPosition, float speedMultiplier = 1f)
    {
        var flat = worldPosition - transform.position;
        flat.y = 0f;
        if (flat.sqrMagnitude < 0.0004f)
            return;

        var step = flat.normalized * (moveSpeed * speedMultiplier * CwslArenaZones.GetMonsterSpeedMultiplier(transform.position)
            * (GetComponent<CwslSlowModifier>()?.SpeedMultiplier ?? 1f) * Time.deltaTime);
        transform.position += step;
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(flat.normalized),
            Time.deltaTime * 10f);
    }

    protected float GetFlatDistanceTo(NetworkObject target)
    {
        if (target == null)
            return float.MaxValue;

        var flat = target.transform.position - transform.position;
        flat.y = 0f;
        return flat.magnitude;
    }
}
