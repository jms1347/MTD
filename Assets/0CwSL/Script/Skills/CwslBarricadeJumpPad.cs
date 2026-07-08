using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

/// <summary>바리케이드 W — 점프 발판. 아군/적군 모두 뛰어오르며 공중에서도 이동 가능.</summary>
public class CwslBarricadeJumpPad : MonoBehaviour
{
    private float radius;
    private float endTime;
    private readonly System.Collections.Generic.Dictionary<int, float> nextJumpById = new();

    public static void SpawnServer(Vector3 center)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            return;

        center.y = 0.05f;
        var go = new GameObject("BarricadeJumpPad");
        go.transform.position = center;
        var pad = go.AddComponent<CwslBarricadeJumpPad>();
        pad.Initialize();
    }

    /// <summary>비서버 클라 전용 시각 — 판정은 서버 SpawnServer만 담당.</summary>
    public static void SpawnVisualOnly(Vector3 center, float lifetime)
    {
        center.y = 0.05f;
        var go = new GameObject("BarricadeJumpPadVisual");
        go.transform.position = center;
        CwslVfxSpawner.SpawnBarricadeJumpPad(center, CwslGameConstants.BarricadeJumpPadRadius, lifetime);
        Object.Destroy(go, lifetime);
    }

    private void Initialize()
    {
        radius = CwslGameConstants.BarricadeJumpPadRadius;
        endTime = Time.time + CwslGameConstants.BarricadeJumpPadLifetime;
        CwslVfxSpawner.SpawnBarricadeJumpPad(transform.position, radius, CwslGameConstants.BarricadeJumpPadLifetime);
        StartCoroutine(TickRoutine());
    }

    private IEnumerator TickRoutine()
    {
        while (Time.time < endTime)
        {
            TryLaunchUnits();
            yield return new WaitForSeconds(0.12f);
        }

        Destroy(gameObject);
    }

    private void TryLaunchUnits()
    {
        var radiusSq = radius * radius;
        var players = CwslCombatRegistry.AlivePlayers;
        foreach (var player in players)
        {
            if (player == null || !player.IsAlive)
                continue;

            TryLaunch(player.transform, player.GetInstanceID(), radiusSq);
        }

        var monsters = CwslCombatRegistry.AliveMonsters;
        foreach (var monster in monsters)
        {
            if (monster == null || !monster.IsAlive)
                continue;

            TryLaunch(monster.transform, monster.GetInstanceID(), radiusSq);
        }
    }

    private void TryLaunch(Transform target, int id, float radiusSq)
    {
        var flat = target.position - transform.position;
        flat.y = 0f;
        if (flat.sqrMagnitude > radiusSq)
            return;

        if (nextJumpById.TryGetValue(id, out var next) && Time.time < next)
            return;

        nextJumpById[id] = Time.time + 1.4f;
        CwslBarricadeJumpController.Ensure(target)?.LaunchServer(
            CwslGameConstants.BarricadeJumpHeight,
            CwslGameConstants.BarricadeJumpDuration);
    }
}

/// <summary>점프 중에도 XZ 이동 가능하도록 Y만 올려 주는 컨트롤러.</summary>
public class CwslBarricadeJumpController : MonoBehaviour
{
    private Coroutine jumpRoutine;
    private float baseY;
    private NavMeshAgent agent;

    public static CwslBarricadeJumpController Ensure(Transform target)
    {
        if (target == null)
            return null;

        var existing = target.GetComponent<CwslBarricadeJumpController>();
        if (existing != null)
            return existing;

        return target.gameObject.AddComponent<CwslBarricadeJumpController>();
    }

    public void LaunchServer(float height, float duration)
    {
        var network = NetworkManager.Singleton;
        if (network == null || !network.IsServer)
            return;

        if (jumpRoutine != null)
            StopCoroutine(jumpRoutine);

        jumpRoutine = StartCoroutine(JumpRoutine(height, duration));
    }

    private IEnumerator JumpRoutine(float height, float duration)
    {
        agent = GetComponent<NavMeshAgent>();
        baseY = transform.position.y;
        var elapsed = 0f;
        var safeDuration = Mathf.Max(0.2f, duration);

        if (agent != null && agent.enabled)
            agent.baseOffset = Mathf.Max(agent.baseOffset, 0.05f);

        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / safeDuration);
            var arc = Mathf.Sin(t * Mathf.PI);
            var pos = transform.position;
            pos.y = baseY + height * arc;
            transform.position = pos;

            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                // XZ 이동은 에이전트가 유지, Y만 시각적으로 보정
            }

            yield return null;
        }

        var end = transform.position;
        end.y = baseY;
        transform.position = end;
        if (agent != null && agent.enabled && agent.isOnNavMesh)
            agent.Warp(end);

        jumpRoutine = null;
    }
}
