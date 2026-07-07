using Unity.Netcode;
using UnityEngine;

/// <summary>ê°€??ê°€ê¹Œìš´ ?Œë ˆ?´ì–´??ë¶€ì°????¬ì?ê°€ ?„ëž˜ë¡??€ë©?3ì´?????°œ.</summary>
public class CwslStickySuicideMonster : CwslSuicideMonster
{
    private const float AttachFuseSeconds = 3f;
    private const float AttachSearchInterval = 0.3f;
    private static readonly Vector3 AttachOffset = new(0f, 1.05f, 0f);

    private bool attached;
    private float fuseTimer;
    private Transform attachHost;
    private float playerSearchTimer;

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
        if (attached)
            SyncAttachStateClientRpc(default, 0f, false);

        attached = false;
        fuseTimer = 0f;
        attachHost = null;
        playerSearchTimer = 0f;

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

        playerSearchTimer -= Time.deltaTime;
        if (playerSearchTimer <= 0f || !IsValidPlayerTarget(currentTarget))
        {
            playerSearchTimer = AttachSearchInterval;
            RefreshPlayerTarget();
        }

        if (!IsValidPlayerTarget(currentTarget))
            return;

        MoveToward(currentTarget.transform.position, RushSpeedMultiplier);

        if (GetFlatDistanceTo(currentTarget) <= 0.95f)
            TryAttachToPlayer(currentTarget.transform);
    }

    private void TickAttached()
    {
        if (attachHost == null)
        {
            DetonateServer();
            return;
        }

        transform.position = attachHost.position + AttachOffset;
        fuseTimer -= Time.deltaTime;
        if (fuseTimer <= 0f)
            DetonateServer();
    }

    private void RefreshPlayerTarget()
    {
        NetworkObject nearest = null;
        var bestDistance = float.MaxValue;
        var position = transform.position;

        foreach (var playerHealth in CwslCombatRegistry.AlivePlayers)
        {
            if (playerHealth == null || !playerHealth.IsAlive)
                continue;

            var playerObject = playerHealth.GetComponent<NetworkObject>();
            if (playerObject == null || !playerObject.IsSpawned)
                continue;

            var flat = playerHealth.transform.position - position;
            flat.y = 0f;
            var distance = flat.sqrMagnitude;
            if (distance >= bestDistance)
                continue;

            bestDistance = distance;
            nearest = playerObject;
        }

        currentTarget = nearest;
    }

    private static bool IsValidPlayerTarget(NetworkObject target)
    {
        if (target == null || !target.IsSpawned)
            return false;

        var playerHealth = target.GetComponent<CwslPlayerHealth>();
        return playerHealth != null && playerHealth.IsAlive;
    }

    private void TryAttachToPlayer(Transform playerTransform)
    {
        if (attached || playerTransform == null)
            return;

        attached = true;
        attachHost = playerTransform;
        fuseTimer = AttachFuseSeconds;

        var collider = GetComponent<CapsuleCollider>();
        if (collider != null)
            collider.enabled = false;

        var hostObject = playerTransform.GetComponent<NetworkObject>();
        SyncAttachStateClientRpc(
            hostObject != null ? new NetworkObjectReference(hostObject) : default,
            AttachFuseSeconds,
            true);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer || detonated || attached)
            return;

        var nexus = other.GetComponentInParent<CwslNexus>();
        if (nexus != null && nexus.IsAlive)
        {
            currentTarget = nexus.GetComponent<NetworkObject>();
            DetonateServer();
            return;
        }

        var playerHealth = other.GetComponentInParent<CwslPlayerHealth>();
        if (playerHealth == null || !playerHealth.IsAlive)
            return;

        currentTarget = playerHealth.GetComponent<NetworkObject>();
        TryAttachToPlayer(playerHealth.transform);
    }

    [ClientRpc]
    private void SyncAttachStateClientRpc(NetworkObjectReference hostRef, float fuseSeconds, bool isAttached)
    {
        var fuseBurn = GetComponentInChildren<CwslStickyMineFuseBurnVisual>(true);
        if (fuseBurn == null)
            return;

        if (!isAttached)
        {
            fuseBurn.StopBurn();
            return;
        }

        Transform hostTransform = null;
        if (hostRef.TryGet(out var hostObject))
            hostTransform = hostObject.transform;

        fuseBurn.BeginAttach(hostTransform, fuseSeconds);
    }
}
