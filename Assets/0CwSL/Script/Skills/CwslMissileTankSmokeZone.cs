using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>?�막??착�? ?�막 ??범위 ?????�턴(감전).</summary>
public class CwslMissileTankSmokeZone : MonoBehaviour
{
    private float radius;
    private float endTime;

    public static void SpawnServer(Vector3 center, float zoneRadius, float duration, CwslMissileTankSkill ownerSkill)
    {
        if (!Unity.Netcode.NetworkManager.Singleton || !Unity.Netcode.NetworkManager.Singleton.IsServer)
            return;

        center.y = 0.05f;
        var zoneObject = new GameObject("MissileTankSmokeZone");
        zoneObject.transform.position = center;
        var zone = zoneObject.AddComponent<CwslMissileTankSmokeZone>();
        zone.Initialize(zoneRadius, duration, ownerSkill);
    }

    private void Initialize(float zoneRadius, float duration, CwslMissileTankSkill ownerSkill)
    {
        radius = zoneRadius;
        endTime = Time.time + duration;
        ownerSkill?.PlaySmokeZoneVisualClientRpc(transform.position, radius, duration);
        StartCoroutine(TickRoutine());
    }

    private IEnumerator TickRoutine()
    {
        var stunDuration = CwslGameConstants.MissileTankSmokeStunDuration;
        while (Time.time < endTime)
        {
            ApplyStunInRadius(stunDuration);
            yield return new WaitForSeconds(0.35f);
        }

        Destroy(gameObject);
    }

    private void ApplyStunInRadius(float stunDuration)
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

            CwslMonsterStatusController.Ensure(monster)?.ApplyShockServer(stunDuration);
        }
    }
}
