using UnityEngine;

[RequireComponent(typeof(Health))]
public class CoopPlayerTowerUnit : MonoBehaviour
{
    private const float BodyY = 0.12f;
    private const float RotateSpeed = 12f;
    private const float ArriveDistance = 0.35f;

    public string PlayerId { get; private set; }
    public string PlayerName { get; private set; }
    public string TankCode { get; private set; }
    public string TankDisplayName { get; private set; }
    public float AttackRange { get; private set; }
    public float BaseMoveSpeed { get; private set; }

    private Health health;
    private Transform hull;
    private Transform turret;
    private bool isLocalOwner;
    private Vector3 syncTargetPosition;
    private bool hasSyncTarget;

    public void Initialize(
        string playerId,
        string playerName,
        CoopTankDefinition tank,
        Health tankHealth,
        Transform hullTransform,
        Transform turretTransform,
        Transform firePoint)
    {
        PlayerId = playerId;
        PlayerName = playerName;
        TankCode = tank.Code;
        TankDisplayName = tank.DisplayName;
        AttackRange = tank.AttackRange;
        BaseMoveSpeed = tank.MoveSpeed;
        health = tankHealth;
        hull = hullTransform;
        turret = turretTransform != null ? turretTransform : hullTransform;

        var lobby = LobbyNetworkManager.Instance;
        var localId = lobby != null ? lobby.LocalPlayerId : CoopGameSession.Instance?.LocalPlayerId;
        isLocalOwner = !string.IsNullOrEmpty(localId) && localId == playerId;
        if (isLocalOwner)
            gameObject.AddComponent<CoopRtsTowerInput>();
    }

    public void ApplyState(CoopPlayerState state, bool snapPosition)
    {
        if (state == null)
            return;

        syncTargetPosition = new Vector3(state.towerX, BodyY, state.towerZ);
        hasSyncTarget = true;

        if (snapPosition)
            ApplyWorldPosition(syncTargetPosition);

        if (health != null)
        {
            if (Mathf.Abs(health.MaxHealth - state.towerMaxHp) > 0.1f)
                health.Initialize(state.towerMaxHp, 0f, 0f);
            health.Heal(state.towerHp - health.CurrentHealth);
        }
    }

    public bool ProcessHostOrder(CoopPlayerState player, float arriveDistance, CoopGameSession session)
    {
        if (player.towerHp <= 0f || player.orderType == CoopGameProtocol.OrderNone)
            return false;

        var current = new Vector3(player.towerX, BodyY, player.towerZ);
        var moveTarget = new Vector3(player.orderX, BodyY, player.orderZ);

        if (player.orderType == CoopGameProtocol.OrderAttackMove
            && session.TryFindEnemyInRangeForUnit(current, AttackRange, out var enemyPos))
        {
            moveTarget = new Vector3(enemyPos.x, BodyY, enemyPos.z);
        }

        if (player.orderType == CoopGameProtocol.OrderAttackTarget
            && player.attackTargetId >= 0
            && session.TryGetEnemyPosition(player.attackTargetId, out var chasePos))
        {
            var flat = chasePos - current;
            flat.y = 0f;
            if (flat.magnitude <= AttackRange)
            {
                FaceTowards(chasePos);
                return false;
            }

            moveTarget = new Vector3(chasePos.x, BodyY, chasePos.z);
        }

        var distance = Vector3.Distance(current, moveTarget);
        var arrived = distance <= arriveDistance;
        if (player.orderType == CoopGameProtocol.OrderAttackMove && arrived)
            arrived = !session.TryFindEnemyInRangeForUnit(current, AttackRange, out _);

        if (arrived)
        {
            if (player.orderType == CoopGameProtocol.OrderMove)
                player.orderType = CoopGameProtocol.OrderNone;
            return false;
        }

        var moveSpeed = BaseMoveSpeed * CoopGimmickBuffs.GetMoveMultiplier(player.playerId);
        if (player.spdLevel > 0)
            moveSpeed += player.spdLevel * 0.35f;

        var next = Vector3.MoveTowards(current, moveTarget, moveSpeed * Time.deltaTime);
        next.y = BodyY;

        player.towerX = next.x;
        player.towerZ = next.z;
        ApplyWorldPosition(next);
        FaceTowards(moveTarget);
        return true;
    }

    private void Update()
    {
        var session = CoopGameSession.Instance;
        if (session == null || !session.TryGetPlayer(PlayerId, out var player))
            return;

        if (session.IsHostAuthority)
        {
            ProcessHostOrder(player, ArriveDistance, session);
            return;
        }

        if (!hasSyncTarget)
            return;

        var next = Vector3.Lerp(transform.position, syncTargetPosition, Time.deltaTime * 14f);
        ApplyWorldPosition(next);

        var flat = syncTargetPosition - transform.position;
        flat.y = 0f;
        if (flat.sqrMagnitude > 0.05f)
            FaceTowards(syncTargetPosition);
    }

    private void ApplyWorldPosition(Vector3 world)
    {
        transform.position = new Vector3(world.x, BodyY, world.z);
    }

    private void FaceTowards(Vector3 worldTarget)
    {
        var flat = worldTarget - transform.position;
        flat.y = 0f;
        if (flat.sqrMagnitude < 0.0001f)
            return;

        var rotation = Quaternion.LookRotation(flat.normalized, Vector3.up);
        if (hull != null)
            hull.rotation = Quaternion.Slerp(hull.rotation, rotation, Time.deltaTime * RotateSpeed);
        if (turret != null)
            turret.rotation = Quaternion.Slerp(turret.rotation, rotation, Time.deltaTime * RotateSpeed * 1.2f);
    }

    public void IssueMove(Vector3 worldPoint)
    {
        IssueOrder(CoopGameProtocol.OrderMove, worldPoint, -1);
    }

    public void IssueAttackMove(Vector3 worldPoint)
    {
        IssueOrder(CoopGameProtocol.OrderAttackMove, worldPoint, -1);
    }

    public void IssueAttackTarget(int enemyId)
    {
        IssueOrder(CoopGameProtocol.OrderAttackTarget, transform.position, enemyId);
    }

    public void IssueSkill(Vector3 worldPoint)
    {
        if (!isLocalOwner)
            return;

        CoopGameSession.Instance?.RequestSkill(PlayerId, worldPoint.x, worldPoint.z);
    }

    public void IssueSkillAtSelf()
    {
        IssueSkill(transform.position);
    }

    private void IssueOrder(int orderType, Vector3 worldPoint, int enemyId)
    {
        if (!isLocalOwner)
            return;

        CoopGameSession.Instance?.RequestOrder(PlayerId, orderType, worldPoint.x, worldPoint.z, enemyId);
    }
}
