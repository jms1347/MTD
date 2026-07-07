using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 홍명보 보스 — HP 380, 4페이즈(흑/백/홍/금) + 다중 공격 패턴.
/// </summary>
public class CwslBossHongmyeongbo : CwslMonsterBase
{
    private enum BossAttackKind
    {
        ProjectileFan,
        GroundSlam,
        SummonAdds,
        RingBurst
    }

    private static readonly BossAttackKind[] Phase1Attacks =
    {
        BossAttackKind.ProjectileFan,
        BossAttackKind.GroundSlam
    };

    private static readonly BossAttackKind[] Phase2Attacks =
    {
        BossAttackKind.ProjectileFan,
        BossAttackKind.SummonAdds,
        BossAttackKind.RingBurst
    };

    private static readonly BossAttackKind[] Phase3Attacks =
    {
        BossAttackKind.GroundSlam,
        BossAttackKind.RingBurst,
        BossAttackKind.ProjectileFan
    };

    private static readonly BossAttackKind[] Phase4Attacks =
    {
        BossAttackKind.ProjectileFan,
        BossAttackKind.GroundSlam,
        BossAttackKind.RingBurst,
        BossAttackKind.SummonAdds
    };

    public static CwslBossHongmyeongbo Active { get; private set; }

    private CwslMonsterHealth bossHealth;
    private CwslBossPhase currentPhase = CwslBossPhase.BlackTeleport;
    private float nextTeleportTime;
    private float nextPhaseBallTime;
    private float nextAttackTime;
    private float teleportResolveTime;
    private Vector3 pendingTeleportPosition;
    private bool teleportPending;
    private bool phaseInitialized;
    private int attackRotation;
    private bool scaleApplied;

    public CwslBossPhase CurrentPhase => currentPhase;

    public override void Initialize(CwslMonsterType type)
    {
        base.Initialize(type);
        moveSpeed = CwslMonsterStatCatalog.BossHongmyeongboMoveSpeed;
        ApplyBossScale();
        bossHealth = GetComponent<CwslMonsterHealth>();
        bossHealth?.ConfigureBoss(CwslMonsterStatCatalog.BossHongmyeongboHealth);
        Active = this;
        currentPhase = CwslBossPhase.BlackTeleport;
        nextTeleportTime = Time.time + 4f;
        nextPhaseBallTime = Time.time + CwslGameConstants.BossPhase2BallInterval;
        nextAttackTime = Time.time + 3f;
        CwslArenaGimmickSystem.Instance?.NotifyBossSpawnedServer();
        EnterPhaseServer(currentPhase, force: true);
        PlayBossSpawnClientRpc();
    }

    public override void OnNetworkDespawn()
    {
        if (Active == this)
            Active = null;
    }

    public void NotifyDamagedServer(float currentHp)
    {
        if (!IsServer)
            return;

        if (currentHp <= CwslGameConstants.BossPhase4Hp && currentPhase < CwslBossPhase.GoldFinal)
            EnterPhaseServer(CwslBossPhase.GoldFinal);
        else if (currentHp <= CwslGameConstants.BossPhase3Hp && currentPhase < CwslBossPhase.RedFight)
            EnterPhaseServer(CwslBossPhase.RedFight);
        else if (currentHp <= CwslGameConstants.BossPhase2Hp && currentPhase < CwslBossPhase.WhiteTeamBall)
            EnterPhaseServer(CwslBossPhase.WhiteTeamBall);
    }

    public static bool CanReceiveDamageFrom(ulong attackerClientId)
    {
        return Active != null;
    }

    protected override void TickServerAI()
    {
        if (teleportPending)
        {
            if (Time.time >= teleportResolveTime)
                CompleteTeleportServer();
            return;
        }

        if (bossHealth == null || !bossHealth.IsAlive)
            return;

        switch (currentPhase)
        {
            case CwslBossPhase.BlackTeleport:
                TickPhase1Server();
                break;
            case CwslBossPhase.WhiteTeamBall:
                TickPhase2Server();
                break;
            case CwslBossPhase.RedFight:
                TickPhase3Server();
                break;
            case CwslBossPhase.GoldFinal:
                TickPhase4Server();
                break;
        }
    }

    private void TickPhase1Server()
    {
        TickExtraAttacksServer(CwslGameConstants.BossAttackIntervalPhase1);

        if (Time.time >= nextTeleportTime)
            BeginTeleportServer(CwslGameConstants.BossPhase1TeleportCooldown);

        ChaseTargetServer(0.75f);
    }

    private void TickPhase2Server()
    {
        TickExtraAttacksServer(CwslGameConstants.BossAttackIntervalPhase2);

        if (Time.time >= nextPhaseBallTime)
        {
            nextPhaseBallTime = Time.time + CwslGameConstants.BossPhase2BallInterval;
            CwslArenaGimmickSystem.Instance?.CastPhase2TeamBallsServer();
        }

        ChaseTargetServer(0.85f);
    }

    private void TickPhase3Server()
    {
        TickExtraAttacksServer(CwslGameConstants.BossAttackIntervalPhase3);

        var fightCenter = new Vector3(
            CwslGameConstants.FightZoneCenterX,
            transform.position.y,
            CwslGameConstants.FightZoneCenterZ);
        MoveToward(fightCenter, 1.1f);

        if (IsValidTarget(currentTarget))
        {
            var flat = currentTarget.transform.position - transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude > 2.5f)
                return;
        }
    }

    private void TickPhase4Server()
    {
        TickExtraAttacksServer(CwslGameConstants.BossAttackIntervalPhase4);

        var center = Vector3.zero;
        center.y = transform.position.y;
        MoveToward(center, 0.55f);
    }

    private void TickExtraAttacksServer(float interval)
    {
        if (GetComponent<BossController>()?.IsCasting == true)
            return;

        if (Time.time < nextAttackTime)
            return;

        nextAttackTime = Time.time + interval;
        ExecuteRotatingAttackServer();
    }

    private void ExecuteRotatingAttackServer()
    {
        var controller = GetComponent<BossController>();
        if (controller != null && controller.TryCastNextSkillServer())
            return;

        var pool = GetAttackPoolForPhase();
        if (pool == null || pool.Length == 0)
            return;

        var kind = pool[attackRotation % pool.Length];
        attackRotation++;

        switch (kind)
        {
            case BossAttackKind.ProjectileFan:
                AttackProjectileFanServer();
                break;
            case BossAttackKind.GroundSlam:
                AttackGroundSlamServer();
                break;
            case BossAttackKind.SummonAdds:
                AttackSummonAddsServer();
                break;
            case BossAttackKind.RingBurst:
                AttackRingBurstServer();
                break;
        }
    }

    private BossAttackKind[] GetAttackPoolForPhase()
    {
        return currentPhase switch
        {
            CwslBossPhase.BlackTeleport => Phase1Attacks,
            CwslBossPhase.WhiteTeamBall => Phase2Attacks,
            CwslBossPhase.RedFight => Phase3Attacks,
            CwslBossPhase.GoldFinal => Phase4Attacks,
            _ => Phase1Attacks
        };
    }

    private void AttackProjectileFanServer()
    {
        var session = CwslGameSession.Instance;
        if (session == null || session.Assets.projectilePrefab == null)
            return;

        var aimDir = transform.forward;
        if (IsValidTarget(currentTarget))
        {
            var flat = currentTarget.transform.position - transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude > 0.01f)
                aimDir = flat.normalized;
        }

        var muzzle = GetMuzzlePosition();
        var count = CwslGameConstants.BossProjectileFanCount;
        var spread = CwslGameConstants.BossProjectileSpreadDegrees;
        var step = count > 1 ? spread / (count - 1) : 0f;
        var startAngle = -spread * 0.5f;

        for (var i = 0; i < count; i++)
        {
            var angle = startAngle + step * i;
            var dir = Quaternion.AngleAxis(angle, Vector3.up) * aimDir;
            FireBossProjectileServer(muzzle, dir);
        }

        PlayProjectileFanClientRpc(muzzle, aimDir);
    }

    private void FireBossProjectileServer(Vector3 muzzle, Vector3 fireDirection)
    {
        var session = CwslGameSession.Instance;
        if (session == null || session.Assets.projectilePrefab == null)
            return;

        if (fireDirection.sqrMagnitude < 0.0001f)
            fireDirection = transform.forward;
        else
            fireDirection.Normalize();

        var networkObject = CwslNetworkPoolService.Instance?.Get(
            session.Assets.projectilePrefab,
            muzzle,
            Quaternion.LookRotation(fireDirection, Vector3.up));
        if (networkObject == null)
            return;

        var projectile = networkObject.GetComponent<CwslMonsterProjectile>();
        projectile?.Configure(
            fireDirection,
            CwslGameConstants.BossProjectileSpeed,
            CwslGameConstants.BossProjectileLifetime);
    }

    private void AttackGroundSlamServer()
    {
        var center = transform.position;
        var radius = CwslGameConstants.BossSlamRadius;
        var radiusSqr = radius * radius;

        foreach (var playerHealth in FindObjectsByType<CwslPlayerHealth>(FindObjectsSortMode.None))
        {
            if (playerHealth == null || !playerHealth.IsAlive)
                continue;

            var flat = playerHealth.transform.position - center;
            flat.y = 0f;
            if (flat.sqrMagnitude > radiusSqr)
                continue;

            playerHealth.TryReceiveExplosionHitServer(CwslGameConstants.BossSlamDamage, playerHealth.transform.position);
        }

        PlayGroundSlamClientRpc(center, radius);
    }

    private void AttackRingBurstServer()
    {
        var center = transform.position;
        var radius = CwslGameConstants.BossRingBurstRadius;
        var radiusSqr = radius * radius;

        foreach (var playerHealth in FindObjectsByType<CwslPlayerHealth>(FindObjectsSortMode.None))
        {
            if (playerHealth == null || !playerHealth.IsAlive)
                continue;

            var flat = playerHealth.transform.position - center;
            flat.y = 0f;
            if (flat.sqrMagnitude > radiusSqr)
                continue;

            playerHealth.TryReceiveExplosionHitServer(CwslGameConstants.BossRingBurstDamage, playerHealth.transform.position);
        }

        PlayRingBurstClientRpc(center, radius);
    }

    private void AttackSummonAddsServer()
    {
        var spawner = CwslGameSession.Instance?.MonsterSpawner;
        if (spawner == null)
            return;

        var count = Random.Range(
            CwslGameConstants.BossSummonCountMin,
            CwslGameConstants.BossSummonCountMax + 1);

        var center = transform.position;
        center.y = CwslGameConstants.SpawnHeight;
        var minRadius = CwslGameConstants.BossSummonMinRadius;
        var maxRadius = minRadius + CwslGameConstants.BossSummonSpread;

        spawner.SpawnMonstersInRingServer(center, count, minRadius, maxRadius, CwslMonsterType.KoreaUniversitySoldier);

        PlaySummonAddsClientRpc(center);
    }

    private Vector3 GetMuzzlePosition()
    {
        var scale = CwslGameConstants.BossVisualScale;
        return transform.position + Vector3.up * (2.2f * scale) + transform.forward * (1.2f * scale);
    }

    private void ApplyBossScale()
    {
        if (scaleApplied)
            return;

        var scale = CwslGameConstants.BossVisualScale;
        if (scale <= 1.01f)
            return;

        scaleApplied = true;

        for (var i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i);
            if (child.GetComponent<Renderer>() == null)
                continue;

            child.localPosition *= scale;
            child.localScale *= scale;
        }

        var collider = GetComponent<CapsuleCollider>();
        if (collider == null)
            return;

        collider.height = 4.2f * scale;
        collider.radius = 1.4f * scale;
        collider.center = new Vector3(0f, 2.1f * scale, 0f);
        bossHealth?.RefreshBossHitCollider();
    }

    private void ChaseTargetServer(float speedMultiplier)
    {
        if (!IsValidTarget(currentTarget))
            return;

        MoveToward(currentTarget.transform.position, speedMultiplier);
    }

    private void BeginTeleportServer(float cooldown)
    {
        nextTeleportTime = Time.time + cooldown;
        pendingTeleportPosition = ResolveFleePositionServer();
        teleportPending = true;
        teleportResolveTime = Time.time + CwslGameConstants.BossTeleportCastSeconds;
        PlayTeleportClientRpc(transform.position, pendingTeleportPosition);
    }

    private void CompleteTeleportServer()
    {
        teleportPending = false;
        transform.position = pendingTeleportPosition;
        PlayTeleportArriveClientRpc(pendingTeleportPosition);
    }

    private void EnterPhaseServer(CwslBossPhase phase, bool force = false)
    {
        if (!IsServer || (phaseInitialized && !force && phase <= currentPhase))
            return;

        phaseInitialized = true;
        currentPhase = phase;
        attackRotation = 0;
        nextAttackTime = Time.time + 2f;
        CwslArenaGimmickSystem.Instance?.EnterBossPhaseServer(phase);

        PlayPhaseTransitionClientRpc(transform.position, (int)phase);

        switch (phase)
        {
            case CwslBossPhase.BlackTeleport:
                nextTeleportTime = Time.time + 4f;
                break;
            case CwslBossPhase.WhiteTeamBall:
                nextPhaseBallTime = Time.time + 2f;
                break;
        }
    }

    private Vector3 ResolveFleePositionServer()
    {
        var centroid = Vector3.zero;
        var count = 0;
        if (NetworkManager.Singleton != null)
        {
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (client.PlayerObject == null)
                    continue;

                centroid += client.PlayerObject.transform.position;
                count++;
            }
        }

        if (count > 0)
            centroid /= count;

        var extent = CwslGameConstants.ArenaHalfExtent - 3f;
        var y = transform.position.y;
        var corners = new[]
        {
            new Vector3(-extent, y, -extent),
            new Vector3(extent, y, -extent),
            new Vector3(-extent, y, extent),
            new Vector3(extent, y, extent)
        };

        var best = corners[0];
        var bestScore = float.MinValue;
        foreach (var corner in corners)
        {
            var score = Vector3.Distance(corner, centroid);
            if (score > bestScore)
            {
                bestScore = score;
                best = corner;
            }
        }

        return CwslArenaUtility.ClampToArena(best);
    }

    private static bool TryGetClientPosition(ulong clientId, out Vector3 position)
    {
        position = Vector3.zero;
        if (NetworkManager.Singleton == null)
            return false;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.ClientId != clientId || client.PlayerObject == null)
                continue;

            position = client.PlayerObject.transform.position;
            return true;
        }

        return false;
    }

    [ClientRpc]
    private void PlayTeleportClientRpc(Vector3 from, Vector3 to)
    {
        CwslVfxSpawner.SpawnBossTeleportDepart(from);
        CwslArenaAudioFeedback.PlayBossTeleportCast(from);
    }

    [ClientRpc]
    private void PlayTeleportArriveClientRpc(Vector3 position)
    {
        CwslVfxSpawner.SpawnBossTeleportArrive(position);
        CwslArenaAudioFeedback.PlayBossTeleportArrive(position);
    }

    [ClientRpc]
    private void PlayPhaseTransitionClientRpc(Vector3 position, int phaseIndex)
    {
        if (System.Enum.IsDefined(typeof(CwslBossPhase), phaseIndex))
            CwslArenaGimmickVisuals.OnBossPhaseChanged((CwslBossPhase)phaseIndex, position);
    }

    [ClientRpc]
    private void PlayProjectileFanClientRpc(Vector3 muzzle, Vector3 aimDirection)
    {
        var rotation = aimDirection.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(aimDirection.normalized, Vector3.up)
            : transform.rotation;
        CwslVfxSpawner.SpawnShadowMuzzleFlash(muzzle, rotation);
        CwslSimpleVfx.SpawnBurst(muzzle, new Color(0.95f, 0.2f, 0.12f), 2.4f, 0.35f);
    }

    [ClientRpc]
    private void PlayGroundSlamClientRpc(Vector3 center, float radius)
    {
        CwslSimpleVfx.SpawnBurst(center, new Color(0.9f, 0.15f, 0.1f), radius * 0.55f, 0.45f);
        CwslArenaAudioFeedback.PlayBossTeleportArrive(center);
    }

    [ClientRpc]
    private void PlayRingBurstClientRpc(Vector3 center, float radius)
    {
        CwslSimpleVfx.SpawnBurst(center, new Color(1f, 0.85f, 0.2f), radius * 0.45f, 0.4f);
        CwslSimpleVfx.SpawnBurst(center + Vector3.up * 0.5f, new Color(0.95f, 0.15f, 0.1f), radius * 0.25f, 0.3f);
    }

    [ClientRpc]
    private void PlaySummonAddsClientRpc(Vector3 center)
    {
        CwslSimpleVfx.SpawnBurst(center, new Color(0.2f, 0.35f, 0.95f), 3.5f, 0.4f);
    }

    [ClientRpc]
    private void PlayBossSpawnClientRpc()
    {
        CwslBossSpawnToast.Show();
        CwslSimpleVfx.SpawnBurst(transform.position + Vector3.up * 6f, new Color(0.95f, 0.15f, 0.1f), 8f, 0.55f);
    }
}
