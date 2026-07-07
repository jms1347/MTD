using Unity.Netcode;
using UnityEngine;

/// <summary>홍명보 「해줘 축구」 안전지대 — 플레이어 자식으로 따라다님 (서버 권한).</summary>
public class CwslBossSafeZone : MonoBehaviour
{
    public static CwslBossSafeZone SpawnOnPlayerServer(NetworkObject playerObject, float durationSeconds)
    {
        if (Unity.Netcode.NetworkManager.Singleton == null || !Unity.Netcode.NetworkManager.Singleton.IsServer
            || playerObject == null)
            return null;

        var safeObject = new GameObject("BossSafeZone");
        safeObject.transform.SetParent(playerObject.transform, false);
        safeObject.transform.localPosition = Vector3.zero;

        var safeZone = safeObject.AddComponent<CwslBossSafeZone>();
        safeZone.Configure(CwslGameConstants.BossSafeZoneRadius);
        var controller = CwslBossHongmyeongbo.Active?.GetComponent<BossController>();
        controller?.NotifySafeZoneSpawnedClientRpc(playerObject, CwslGameConstants.BossSafeZoneRadius);
        Object.Destroy(safeObject, durationSeconds);
        return safeZone;
    }

    private void Configure(float radius)
    {
        var trigger = gameObject.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = radius;
        trigger.center = Vector3.zero;

        var tracker = gameObject.AddComponent<CwslBossSafeZoneTracker>();
        tracker.Initialize(this);
    }

    public void OnPlayerEntered(CwslPlayerBossDebuff debuff)
    {
        debuff?.EnterBossSafeZoneServer();
    }

    public void OnPlayerExited(CwslPlayerBossDebuff debuff)
    {
        debuff?.ExitBossSafeZoneServer();
    }
}

/// <summary>안전지대 트리거 추적.</summary>
public class CwslBossSafeZoneTracker : MonoBehaviour
{
    private CwslBossSafeZone safeZone;

    public void Initialize(CwslBossSafeZone zone)
    {
        safeZone = zone;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!Unity.Netcode.NetworkManager.Singleton.IsServer || safeZone == null)
            return;

        var debuff = other.GetComponent<CwslPlayerBossDebuff>()
                     ?? other.GetComponentInParent<CwslPlayerBossDebuff>();
        if (debuff == null)
            return;

        safeZone.OnPlayerEntered(debuff);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!Unity.Netcode.NetworkManager.Singleton.IsServer || safeZone == null)
            return;

        var debuff = other.GetComponent<CwslPlayerBossDebuff>()
                     ?? other.GetComponentInParent<CwslPlayerBossDebuff>();
        if (debuff == null)
            return;

        safeZone.OnPlayerExited(debuff);
    }
}
