using System.Collections;
using UnityEngine;

/// <summary>홍명보 「전술의 부재」 경고 장판 — 서버 전용.</summary>
public class CwslBossWarningZone : MonoBehaviour
{
    public static void SpawnServer(Vector3 position, float radius, float telegraphSeconds, float damage, float reverseDuration)
    {
        if (Unity.Netcode.NetworkManager.Singleton == null || !Unity.Netcode.NetworkManager.Singleton.IsServer)
            return;

        CwslBossSkillVfx.ShowWarningZone(position, radius, telegraphSeconds);

        var zoneObject = new GameObject("BossWarningZone");
        zoneObject.transform.position = position;
        var zone = zoneObject.AddComponent<CwslBossWarningZone>();
        zone.StartCoroutine(zone.RunServer(position, radius, telegraphSeconds, damage, reverseDuration));
    }

    private IEnumerator RunServer(Vector3 center, float radius, float telegraphSeconds, float damage, float reverseDuration)
    {
        yield return new WaitForSeconds(telegraphSeconds);

        var hits = Physics.OverlapSphere(
            center,
            radius,
            LayerMask.GetMask(CwslGameConstants.LayerPlayer),
            QueryTriggerInteraction.Collide);

        foreach (var hit in hits)
        {
            var playerHealth = hit.GetComponent<CwslPlayerHealth>() ?? hit.GetComponentInParent<CwslPlayerHealth>();
            if (playerHealth == null || !playerHealth.IsAlive)
                continue;

            playerHealth.TryReceiveExplosionHitServer(damage, center);
            var debuff = playerHealth.GetComponent<CwslPlayerBossDebuff>();
            if (debuff != null)
                debuff.ApplyReverseControlServer(reverseDuration);
        }

        CwslBossSkillVfx.ShowExplosion(center, radius);
        Destroy(gameObject);
    }
}
