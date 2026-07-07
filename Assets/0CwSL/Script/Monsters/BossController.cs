using System.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 홍명보 최종 보스 스킬 컨트롤러 — 5가지 패턴을 Coroutine으로 순환 실행.
/// </summary>
[RequireComponent(typeof(CwslBossHongmyeongbo))]
public class BossController : NetworkBehaviour
{
    private enum BossSkillId
    {
        SummonElites = 0,
        SilenceWatching = 1,
        ReverseTactics = 2,
        DoItForMe = 3,
        IsThisATeam = 4
    }

    private static readonly BossSkillId[] SkillRotation =
    {
        BossSkillId.SummonElites,
        BossSkillId.SilenceWatching,
        BossSkillId.ReverseTactics,
        BossSkillId.DoItForMe,
        BossSkillId.IsThisATeam
    };

    private CwslBossHongmyeongbo boss;
    private bool isCasting;
    private int skillIndex;

    private void Awake()
    {
        boss = GetComponent<CwslBossHongmyeongbo>();
    }

    /// <summary>보스 AI 틱에서 호출 — 스킬 캐스트를 시작하면 true.</summary>
    public bool TryCastNextSkillServer()
    {
        if (!IsServer || isCasting || boss == null)
            return false;

        var skill = SkillRotation[skillIndex % SkillRotation.Length];
        skillIndex++;
        StartCoroutine(CastSkillRoutine(skill));
        return true;
    }

    public bool IsCasting => isCasting;

    private IEnumerator CastSkillRoutine(BossSkillId skill)
    {
        isCasting = true;

        switch (skill)
        {
            case BossSkillId.SummonElites:
                yield return Skill_SummonElites();
                break;
            case BossSkillId.SilenceWatching:
                yield return Skill_SilenceWatching();
                break;
            case BossSkillId.ReverseTactics:
                yield return Skill_ReverseTactics();
                break;
            case BossSkillId.DoItForMe:
                yield return Skill_DoItForMe();
                break;
            case BossSkillId.IsThisATeam:
                yield return Skill_IsThisATeam();
                break;
        }

        isCasting = false;
    }

    /// <summary>싸워 — 고려대 선수 소환.</summary>
    private IEnumerator Skill_SummonElites()
    {
        NotifySkillNameClientRpc("싸워");

        if (!CwslTargetQuery.TryGetLowestHpLivingPlayer(out var target, out _))
        {
            yield return new WaitForSeconds(0.5f);
            yield break;
        }

        var edgeCenter = GetRandomMapCorner();
        var spawner = CwslGameSession.Instance?.MonsterSpawner;
        if (spawner != null)
            spawner.SpawnBossElitePackAtEdgeServer(edgeCenter, target);

        PlaySummonVfxClientRpc(edgeCenter);
        yield return new WaitForSeconds(1.2f);
    }

    /// <summary>홍명보가 쳐다보고 있다 — 침묵 10초.</summary>
    private IEnumerator Skill_SilenceWatching()
    {
        NotifySkillNameClientRpc("홍명보가 쳐다보고 있다");

        var watch = CwslBossWatchState.Instance;
        if (watch != null && watch.TryStartWatchServer())
        {
            var watchedId = watch.WatchedClientId;
            NotifySilenceEyeClientRpc(watchedId, CwslGameConstants.BossWatchDuration);
        }

        yield return new WaitForSeconds(CwslGameConstants.BossWatchDuration);
    }

    /// <summary>전술의 부재 — 경고 장판 후 폭발 + 방향 반전.</summary>
    private IEnumerator Skill_ReverseTactics()
    {
        NotifySkillNameClientRpc("전술의 부재");

        var zoneCount = Random.Range(
            CwslGameConstants.BossReverseZoneCountMin,
            CwslGameConstants.BossReverseZoneCountMax + 1);

        for (var i = 0; i < zoneCount; i++)
        {
            var position = GetRandomArenaGroundPoint();
            CwslBossWarningZone.SpawnServer(
                position,
                CwslGameConstants.BossReverseZoneRadius,
                CwslGameConstants.BossReverseTelegraphSeconds,
                CwslGameConstants.BossReverseExplosionDamage,
                CwslGameConstants.BossReverseControlDuration);
        }

        yield return new WaitForSeconds(
            CwslGameConstants.BossReverseTelegraphSeconds + CwslGameConstants.BossReverseCastBuffer);
    }

    /// <summary>해줘 축구 — 맵 중앙 탄막 + 이동 안전지대.</summary>
    private IEnumerator Skill_DoItForMe()
    {
        NotifySkillNameClientRpc("해줘 축구");

        var center = new Vector3(0f, CwslGameConstants.SpawnHeight, 0f);
        yield return MoveBossToServer(center, 1.8f);

        NetworkObject safePlayer = null;
        if (CwslTargetQuery.TryGetRandomLivingPlayer(out var randomPlayer))
            safePlayer = randomPlayer;

        if (safePlayer != null)
            CwslBossSafeZone.SpawnOnPlayerServer(safePlayer, CwslGameConstants.BossBarrageDuration);

        var endTime = Time.time + CwslGameConstants.BossBarrageDuration;
        while (Time.time < endTime)
        {
            FireBarrageRingServer(center);
            yield return new WaitForSeconds(CwslGameConstants.BossBarrageInterval);
        }
    }

    /// <summary>이게 팀이야 — 감염 구체 3발.</summary>
    private IEnumerator Skill_IsThisATeam()
    {
        NotifySkillNameClientRpc("이게 팀이야");

        var aimDirections = ResolveInfectionOrbDirections();
        var muzzle = transform.position + Vector3.up * (2f * CwslGameConstants.BossVisualScale);

        for (var i = 0; i < CwslGameConstants.BossInfectionOrbCount; i++)
        {
            var dir = aimDirections[Mathf.Min(i, aimDirections.Length - 1)];
            CwslBossSkillProjectile.SpawnServer(
                muzzle,
                dir,
                CwslBossSkillProjectileKind.InfectionOrb);
            yield return new WaitForSeconds(0.35f);
        }

        yield return new WaitForSeconds(0.8f);
    }

    private void FireBarrageRingServer(Vector3 origin)
    {
        var count = CwslGameConstants.BossBarrageProjectileCount;
        var muzzleY = origin + Vector3.up * (1.5f * CwslGameConstants.BossVisualScale);

        for (var i = 0; i < count; i++)
        {
            var angle = 360f * i / count;
            var dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
            CwslBossSkillProjectile.SpawnServer(
                muzzleY,
                dir,
                CwslBossSkillProjectileKind.Barrage);
        }

        PlayBarrageWaveClientRpc(muzzleY);
    }

    private Vector3[] ResolveInfectionOrbDirections()
    {
        var directions = new System.Collections.Generic.List<Vector3>();
        if (NetworkManager.Singleton != null)
        {
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (client.PlayerObject == null || !client.PlayerObject.IsSpawned)
                    continue;

                var health = client.PlayerObject.GetComponent<CwslPlayerHealth>();
                if (health != null && !health.IsAlive)
                    continue;

                var flat = client.PlayerObject.transform.position - transform.position;
                flat.y = 0f;
                if (flat.sqrMagnitude > 0.01f)
                    directions.Add(flat.normalized);
            }
        }

        while (directions.Count < CwslGameConstants.BossInfectionOrbCount)
            directions.Add(Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward);

        return directions.ToArray();
    }

    private IEnumerator MoveBossToServer(Vector3 destination, float duration)
    {
        var start = transform.position;
        destination.y = start.y;
        var elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            transform.position = Vector3.Lerp(start, destination, t);
            yield return null;
        }

        transform.position = destination;
    }

    private static Vector3 GetRandomMapCorner()
    {
        var edge = CwslArenaUtility.GetMapEdgeHalfExtent(CwslGameConstants.MapEdgeSpawnInset);
        var corners = new[]
        {
            new Vector3(-edge, CwslGameConstants.SpawnHeight, -edge),
            new Vector3(edge, CwslGameConstants.SpawnHeight, -edge),
            new Vector3(-edge, CwslGameConstants.SpawnHeight, edge),
            new Vector3(edge, CwslGameConstants.SpawnHeight, edge)
        };

        var corner = corners[Random.Range(0, corners.Length)];
        var jitter = Random.insideUnitCircle * 2.5f;
        corner.x += jitter.x;
        corner.z += jitter.y;
        return CwslArenaUtility.ClampToArena(corner);
    }

    private static Vector3 GetRandomArenaGroundPoint()
    {
        var extent = CwslGameConstants.ArenaHalfExtent - 4f;
        var point = new Vector3(
            Random.Range(-extent, extent),
            CwslGameConstants.SpawnHeight,
            Random.Range(-extent, extent));
        return CwslArenaUtility.ClampToArena(point);
    }

    [ClientRpc]
    private void NotifySkillNameClientRpc(string skillName)
    {
        CwslBossSpawnToast.ShowSkill(skillName);
    }

    [ClientRpc]
    private void NotifySilenceEyeClientRpc(ulong clientId, float duration)
    {
        if (!TryGetClientTransform(clientId, out var playerTransform))
            return;

        CwslBossSkillVfx.ShowSilenceEye(playerTransform, duration);
    }

    [ClientRpc]
    private void PlaySummonVfxClientRpc(Vector3 position)
    {
        CwslSimpleVfx.SpawnBurst(position, new Color(0.2f, 0.35f, 0.95f), 4f, 0.45f);
    }

    [ClientRpc]
    private void PlayBarrageWaveClientRpc(Vector3 position)
    {
        CwslSimpleVfx.SpawnBurst(position, new Color(0.95f, 0.2f, 0.1f), 3f, 0.2f);
    }

    private static bool TryGetClientTransform(ulong clientId, out Transform playerTransform)
    {
        playerTransform = null;
        if (NetworkManager.Singleton == null)
            return false;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.ClientId != clientId || client.PlayerObject == null)
                continue;

            playerTransform = client.PlayerObject.transform;
            return true;
        }

        return false;
    }

    [ClientRpc]
    public void NotifySafeZoneSpawnedClientRpc(NetworkObjectReference parentRef, float radius)
    {
        if (!parentRef.TryGet(out var parentObject))
            return;

        CwslBossSkillVfx.AttachSafeZoneVisual(parentObject.transform, radius);
    }
}
