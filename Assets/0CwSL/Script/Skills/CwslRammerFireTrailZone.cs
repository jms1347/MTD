using System.Collections;
using UnityEngine;

/// <summary>질주자 R 바닥 불길 — 접촉 몬스터에게 화상.</summary>
public class CwslRammerFireTrailZone : MonoBehaviour
{
    private float radius;
    private float endTime;
    private ulong ownerClientId;

    public static void SpawnServer(Vector3 center, ulong ownerClientId)
    {
        if (Unity.Netcode.NetworkManager.Singleton == null ||
            !Unity.Netcode.NetworkManager.Singleton.IsServer)
            return;

        center.y = 0.05f;
        var go = new GameObject("RammerFireTrailZone");
        go.transform.position = center;
        var zone = go.AddComponent<CwslRammerFireTrailZone>();
        zone.Initialize(ownerClientId);
    }

    private void Initialize(ulong owner)
    {
        ownerClientId = owner;
        radius = CwslGameConstants.RammerFireTrailZoneRadius;
        endTime = Time.time + CwslGameConstants.RammerFireTrailZoneLifetime;
        StartCoroutine(TickRoutine());
    }

    private IEnumerator TickRoutine()
    {
        while (Time.time < endTime)
        {
            ApplyBurnInRadius();
            yield return new WaitForSeconds(0.35f);
        }

        Destroy(gameObject);
    }

    private void ApplyBurnInRadius()
    {
        var radiusSq = radius * radius;
        var monsters = CwslCombatRegistry.AliveMonsters;
        foreach (var monster in monsters)
        {
            if (monster == null || !monster.IsAlive)
                continue;

            var flat = monster.transform.position - transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude > radiusSq)
                continue;

            CwslMonsterStatusController.Ensure(monster)?.ApplyBurnServer(
                ownerClientId,
                CwslGameConstants.MonsterBurnDuration,
                CwslCombatMath.ResolveSkillDamageForClient(
                    ownerClientId,
                    CwslGameConstants.MonsterBurnTotalSkillCoeff));
        }
    }
}
