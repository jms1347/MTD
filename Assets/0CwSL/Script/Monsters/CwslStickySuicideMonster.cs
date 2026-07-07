using Unity.Netcode;
using UnityEngine;

/// <summary>가장 가까운 아군 몬스터에 부착 후 3초 뒤 폭발.</summary>
public class CwslStickySuicideMonster : CwslSuicideMonster
{
    private const float AttachFuseSeconds = 3f;
    private const float AttachSearchInterval = 0.3f;
    private static readonly Vector3 AttachOffset = new(0f, 1.05f, 0f);

    private bool attached;
    private float fuseTimer;
    private Transform attachHost;
    private float allySearchTimer;

    public override void Initialize(CwslMonsterType type)
    {
        base.Initialize(type);
        ResetStickyState();
    }

    protected override void ResetSuicideState()
    {
        ResetStickyState();
    }

    private void ResetStickyState()
    {
        attached = false;
        fuseTimer = 0f;
        attachHost = null;
        allySearchTimer = 0f;
        var collider = GetComponent<CapsuleCollider>();
        if (collider != null)
            collider.enabled = true;
    }

    protected override void TickServerAI()
    {
        if (detonated)
            return;

        if (attached)
        {
            TickAttached();
            return;
        }

        allySearchTimer -= Time.deltaTime;
        if (allySearchTimer <= 0f || !IsValidAllyTarget(currentTarget))
        {
            allySearchTimer = AttachSearchInterval;
            RefreshAllyTarget();
        }

        if (!IsValidAllyTarget(currentTarget))
            return;

        MoveToward(currentTarget.transform.position, RushSpeedMultiplier);

        if (GetFlatDistanceTo(currentTarget) <= 0.95f)
            TryAttachToAlly(currentTarget.transform);
    }

    private void TickAttached()
    {
        if (attachHost == null)
        {
            DetonateServer();
            return;
        }

        transform.position = attachHost.position + Vector3.up * AttachOffset.y;
        fuseTimer -= Time.deltaTime;
        if (fuseTimer <= 0f)
            DetonateServer();
    }

    private void RefreshAllyTarget()
    {
        NetworkObject nearest = null;
        var bestDistance = float.MaxValue;
        var position = transform.position;

        foreach (var allyHealth in Object.FindObjectsByType<CwslMonsterHealth>(FindObjectsSortMode.None))
        {
            if (allyHealth == null || allyHealth == health || !allyHealth.IsAlive)
                continue;

            if (allyHealth.MonsterType is CwslMonsterType.StickySuicide
                or CwslMonsterType.Suicide
                or CwslMonsterType.NexusSuicide
                or CwslMonsterType.BossHongmyeongbo
                or CwslMonsterType.DefenseBoss
                or CwslMonsterType.MidBoss)
                continue;

            var allyObject = allyHealth.GetComponent<NetworkObject>();
            if (allyObject == null || !allyObject.IsSpawned)
                continue;

            var flat = allyHealth.transform.position - position;
            flat.y = 0f;
            var distance = flat.sqrMagnitude;
            if (distance >= bestDistance)
                continue;

            bestDistance = distance;
            nearest = allyObject;
        }

        currentTarget = nearest;
    }

    private static bool IsValidAllyTarget(NetworkObject target)
    {
        if (target == null || !target.IsSpawned)
            return false;

        var allyHealth = target.GetComponent<CwslMonsterHealth>();
        return allyHealth != null && allyHealth.IsAlive;
    }

    private void TryAttachToAlly(Transform allyTransform)
    {
        if (attached || allyTransform == null)
            return;

        attached = true;
        attachHost = allyTransform;
        fuseTimer = AttachFuseSeconds;

        var collider = GetComponent<CapsuleCollider>();
        if (collider != null)
            collider.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer || detonated || attached)
            return;

        var allyHealth = other.GetComponentInParent<CwslMonsterHealth>();
        if (allyHealth == null || allyHealth == health || !allyHealth.IsAlive)
            return;

        if (allyHealth.MonsterType is CwslMonsterType.StickySuicide
            or CwslMonsterType.Suicide
            or CwslMonsterType.NexusSuicide
            or CwslMonsterType.BossHongmyeongbo
            or CwslMonsterType.DefenseBoss
            or CwslMonsterType.MidBoss)
            return;

        currentTarget = allyHealth.GetComponent<NetworkObject>();
        TryAttachToAlly(allyHealth.transform);
    }
}
