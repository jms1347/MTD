using Unity.Netcode;
using UnityEngine;

public enum StllEnemyKind : byte
{
    Grunt = 0,
    Archer = 1,
    Charger = 2,
    Arsonist = 3,
    EliteGuard = 4
}

/// <summary>사수관 적 — 군량고 우선, 근접 시 공격.</summary>
public class StllEnemyGruntAI : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 4.2f;
    [SerializeField] private float attackRange = 1.4f;
    [SerializeField] private float attackDamage = 12f;
    [SerializeField] private float attackInterval = 1.1f;

    private readonly NetworkVariable<byte> enemyKind = new(
        (byte)StllEnemyKind.Grunt,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private StllEnemyHealth health;
    private float nextAttackTime;
    private Transform forcedTarget;
    private float forcedTargetUntil;

    public StllEnemyKind Kind => (StllEnemyKind)enemyKind.Value;

    public void ForceTauntTargetServer(Transform target, float duration)
    {
        if (!IsServer)
            return;

        forcedTarget = target;
        forcedTargetUntil = Time.time + duration;
    }

    private void Awake()
    {
        health = GetComponent<StllEnemyHealth>();
    }

    public void ConfigureServer(StllEnemyKind kind, float maxHealth, Color bodyColor)
    {
        if (!IsServer)
            return;

        enemyKind.Value = (byte)kind;
        health.ConfigureServer(maxHealth);

        if (kind == StllEnemyKind.Charger)
            moveSpeed = 6.2f;
        else if (kind == StllEnemyKind.Archer)
            moveSpeed = 3.6f;
    }

    private void Update()
    {
        if (!IsServer || health == null || !health.IsAlive)
            return;

        var target = FindTarget();
        if (target == null)
            return;

        var toTarget = target.position - transform.position;
        toTarget.y = 0f;
        var distance = toTarget.magnitude;

        if (distance <= attackRange)
        {
            TryAttackTarget(target);
            return;
        }

        var direction = toTarget.normalized;
        transform.position += direction * (moveSpeed * Time.deltaTime);
        if (direction.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 8f * Time.deltaTime);
    }

    private Transform FindTarget()
    {
        if (forcedTarget != null && Time.time < forcedTargetUntil)
            return forcedTarget;

        StllSupplyDepot nearestDepot = null;
        var nearestDist = float.MaxValue;
        var depots = FindObjectsByType<StllSupplyDepot>(FindObjectsSortMode.None);
        for (var i = 0; i < depots.Length; i++)
        {
            var depot = depots[i];
            if (depot == null || !depot.IsAlive)
                continue;

            var dist = Vector3.Distance(transform.position, depot.transform.position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearestDepot = depot;
            }
        }

        if (nearestDepot != null)
            return nearestDepot.transform;

        var players = FindObjectsByType<StllBrotherhoodRoleState>(FindObjectsSortMode.None);
        Transform nearestPlayer = null;
        for (var i = 0; i < players.Length; i++)
        {
            var player = players[i];
            if (player == null)
                continue;

            var dist = Vector3.Distance(transform.position, player.transform.position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearestPlayer = player.transform;
            }
        }

        return nearestPlayer;
    }

    private void TryAttackTarget(Transform target)
    {
        if (Time.time < nextAttackTime)
            return;

        nextAttackTime = Time.time + attackInterval;

        var depot = target.GetComponent<StllSupplyDepot>();
        if (depot != null && depot.IsAlive)
        {
            depot.DamageServer(attackDamage);
            return;
        }

        var player = target.GetComponent<StllPlayerHealth>();
        if (player != null && player.IsAlive)
            player.DamageServer(attackDamage);
    }
}
