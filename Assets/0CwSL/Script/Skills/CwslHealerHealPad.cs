using System.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>힐러 Q — 아군(자신 포함) 초당 회복 장판.</summary>
public class CwslHealerHealPad : MonoBehaviour
{
    private float radius;
    private float endTime;

    public static void SpawnServer(Vector3 center)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            return;

        center.y = 0.05f;
        var go = new GameObject("HealerHealPad");
        go.transform.position = center;
        var pad = go.AddComponent<CwslHealerHealPad>();
        pad.Initialize();
    }

    private void Initialize()
    {
        radius = CwslGameConstants.HealerHealPadRadius;
        endTime = Time.time + CwslGameConstants.HealerHealPadDuration;
        CwslVfxSpawner.SpawnHealerHealPad(transform.position, radius, CwslGameConstants.HealerHealPadDuration);
        StartCoroutine(TickRoutine());
    }

    private IEnumerator TickRoutine()
    {
        while (Time.time < endTime)
        {
            HealAllies();
            yield return new WaitForSeconds(0.5f);
        }

        Destroy(gameObject);
    }

    private void HealAllies()
    {
        var heal = CwslGameConstants.HealerHealPadHealPerSecond * 0.5f;
        var radiusSq = radius * radius;
        var players = CwslCombatRegistry.AlivePlayers;
        foreach (var player in players)
        {
            if (player == null || !player.IsAlive)
                continue;

            var flat = player.transform.position - transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude > radiusSq)
                continue;

            player.TryHealServer(heal);
        }
    }
}
