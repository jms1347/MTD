using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 홍명보 보스 — HP 380, 4페이즈(흑/백/홍/금) + 사귀 돌 연동.
/// </summary>
public class CwslBossHongmyeongbo : CwslMonsterBase
{
    public static CwslBossHongmyeongbo Active { get; private set; }

    private CwslMonsterHealth bossHealth;
    private CwslBossPhase currentPhase = CwslBossPhase.BlackTeleport;
    private float nextTeleportTime;
    private float nextPhaseBallTime;
    private float teleportResolveTime;
    private Vector3 pendingTeleportPosition;
    private bool teleportPending;
    private bool phaseInitialized;

    public CwslBossPhase CurrentPhase => currentPhase;

    public override void Initialize(CwslMonsterType type)
    {
        base.Initialize(type);
        moveSpeed = 2.2f;
        bossHealth = GetComponent<CwslMonsterHealth>();
        bossHealth?.ConfigureBoss(CwslGameConstants.BossMaxHealth);
        Active = this;
        currentPhase = CwslBossPhase.BlackTeleport;
        nextTeleportTime = Time.time + 4f;
        nextPhaseBallTime = Time.time + CwslGameConstants.BossPhase2BallInterval;
        CwslArenaGimmickSystem.Instance?.NotifyBossSpawnedServer();
        EnterPhaseServer(currentPhase, force: true);
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
        if (Active == null || Active.currentPhase != CwslBossPhase.RedFight)
            return true;

        if (!TryGetClientPosition(attackerClientId, out var position))
            return false;

        return CwslArenaZones.IsInFightZone(position);
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
        if (Time.time >= nextTeleportTime)
            BeginTeleportServer(CwslGameConstants.BossPhase1TeleportCooldown);

        ChaseTargetServer(0.75f);
    }

    private void TickPhase2Server()
    {
        if (Time.time >= nextPhaseBallTime)
        {
            nextPhaseBallTime = Time.time + CwslGameConstants.BossPhase2BallInterval;
            CwslArenaGimmickSystem.Instance?.CastPhase2TeamBallsServer();
        }

        ChaseTargetServer(0.85f);
    }

    private void TickPhase3Server()
    {
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
        var center = Vector3.zero;
        center.y = transform.position.y;
        MoveToward(center, 0.55f);
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

    private static Vector3 ResolveFleePositionServer()
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
        var corners = new[]
        {
            new Vector3(-extent, 1.6f, -extent),
            new Vector3(extent, 1.6f, -extent),
            new Vector3(-extent, 1.6f, extent),
            new Vector3(extent, 1.6f, extent)
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
}
