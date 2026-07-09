using System.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>힐러 W — 독 장판. 초당 중첩, 지속 3초.</summary>
public class CwslHealerPoisonPad : MonoBehaviour
{
    private float radius;
    private float endTime;
    private ulong ownerClientId;

    public static void SpawnServer(Vector3 center, ulong ownerClientId)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            return;

        center.y = 0.05f;
        var go = new GameObject("HealerPoisonPad");
        go.transform.position = center;
        var pad = go.AddComponent<CwslHealerPoisonPad>();
        pad.Initialize(ownerClientId);
    }

    private void Initialize(ulong owner)
    {
        ownerClientId = owner;
        radius = CwslGameConstants.HealerPoisonPadRadius;
        endTime = Time.time + CwslGameConstants.HealerPoisonPadDuration;
        CwslVfxSpawner.SpawnHealerPoisonPad(transform.position, radius, CwslGameConstants.HealerPoisonPadDuration);
        StartCoroutine(TickRoutine());
    }

    private IEnumerator TickRoutine()
    {
        while (Time.time < endTime)
        {
            ApplyPoison();
            yield return new WaitForSeconds(1f);
        }

        Destroy(gameObject);
    }

    private void ApplyPoison()
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

            CwslMonsterStatusController.Ensure(monster)?.ApplyPoisonServer(
                ownerClientId,
                CwslGameConstants.HealerPoisonDuration,
                CwslCombatMath.ResolveSkillDamageForClient(
                    ownerClientId,
                    CwslGameConstants.HealerPoisonTickSkillCoeff),
                CwslGameConstants.HealerPoisonArmorPerStack);
        }
    }
}
