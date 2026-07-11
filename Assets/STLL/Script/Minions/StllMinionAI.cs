using Unity.Netcode;
using UnityEngine;

public enum StllMinionCommandMode : byte
{
    Follow = 0,
    Hold = 1
}

/// <summary>부하 병사 AI — 수행/사수 모드.</summary>
public class StllMinionAI : NetworkBehaviour
{
    [SerializeField] private float attackDamage = StllGlaiveConstants.MinionBaseDamage;

    private NetworkObject commanderObject;
    private Vector3 holdPosition;
    private Vector3 followOffset;
    private StllMinionCommandMode mode = StllMinionCommandMode.Follow;
    private float nextAttackTime;
    private StllCommanderAura auraSource;

    public void AssignCommanderServer(NetworkObject commander, StllMinionCommandMode initialMode, Vector3 holdPos)
    {
        if (!IsServer)
            return;

        commanderObject = commander;
        mode = initialMode;
        holdPosition = holdPos;
        followOffset = new Vector3(Random.Range(-1.2f, 1.2f), 0f, Random.Range(-2.2f, -0.6f));
        auraSource = commander != null ? commander.GetComponent<StllCommanderAura>() : null;
    }

    public void SetModeServer(StllMinionCommandMode newMode, Vector3 holdPos)
    {
        if (!IsServer)
            return;

        mode = newMode;
        holdPosition = holdPos;
    }

    private void Update()
    {
        if (!IsServer || commanderObject == null)
            return;

        var commander = commanderObject.transform;
        if (commander == null)
            return;

        switch (mode)
        {
            case StllMinionCommandMode.Follow:
                TickFollow(commander);
                break;
            case StllMinionCommandMode.Hold:
                TickHold();
                break;
        }
    }

    private void TickFollow(Transform commander)
    {
        var target = commander.position + commander.TransformDirection(followOffset);
        MoveToward(target);

        TryAttackNearby();
    }

    private void TickHold()
    {
        MoveToward(holdPosition);
        TryAttackNearby();
    }

    private void MoveToward(Vector3 target)
    {
        var flat = target - transform.position;
        flat.y = 0f;
        if (flat.sqrMagnitude < 0.08f)
            return;

        var step = flat.normalized * (StllGlaiveConstants.MinionMoveSpeed * Time.deltaTime);
        transform.position += step;
        if (flat.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(flat.normalized, Vector3.up);
    }

    private void TryAttackNearby()
    {
        if (Time.time < nextAttackTime)
            return;

        var damage = attackDamage;
        if (auraSource != null)
            damage *= auraSource.GetMinionDamageMultiplier(transform.position);

        var hits = Physics.OverlapSphere(transform.position, StllGlaiveConstants.MinionAttackRange);
        for (var i = 0; i < hits.Length; i++)
        {
            var enemy = hits[i].GetComponentInParent<StllEnemyHealth>();
            if (enemy == null || !enemy.IsAlive)
                continue;

            enemy.TakeDamageServer(damage, commanderObject.OwnerClientId, Vector3.zero);
            nextAttackTime = Time.time + 0.65f;
            return;
        }
    }
}
