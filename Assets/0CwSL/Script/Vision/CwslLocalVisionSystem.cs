using Unity.Netcode;
using UnityEngine;

public class CwslLocalVisionSystem : MonoBehaviour
{
    private const float RefreshInterval = 0.08f;

    private CwslPlayerVision playerVision;
    private float refreshTimer;

    private void Awake()
    {
        playerVision = GetComponent<CwslPlayerVision>();
    }

    private void Update()
    {
        if (playerVision == null || !playerVision.IsOwner)
            return;

        refreshTimer -= Time.deltaTime;
        if (refreshTimer > 0f)
            return;

        refreshTimer = RefreshInterval;
        RefreshVisibility();
    }

    private void RefreshVisibility()
    {
        var origin = playerVision.VisionOrigin;
        var radiusSqr = playerVision.VisionRadius * playerVision.VisionRadius;

        RefreshMonsters(origin, radiusSqr);
        RefreshGold(origin, radiusSqr);
        RefreshOtherPlayers(origin, radiusSqr);
    }

    private void RefreshMonsters(Vector3 origin, float radiusSqr)
    {
        var monsters = FindObjectsByType<CwslMonsterBase>(FindObjectsSortMode.None);
        foreach (var monster in monsters)
        {
            if (monster == null)
                continue;

            var health = monster.GetComponent<CwslMonsterHealth>();
            if (health != null && !health.IsAlive)
            {
                SetOccludeeVisible(monster.gameObject, false);
                continue;
            }

            SetOccludeeVisible(monster.gameObject, IsInRadius(origin, monster.transform.position, radiusSqr));
        }
    }

    private void RefreshGold(Vector3 origin, float radiusSqr)
    {
        var pickups = FindObjectsByType<CwslGoldPickup>(FindObjectsSortMode.None);
        foreach (var pickup in pickups)
        {
            if (pickup == null)
                continue;

            SetOccludeeVisible(pickup.gameObject, IsInRadius(origin, pickup.transform.position, radiusSqr));
        }
    }

    private void RefreshOtherPlayers(Vector3 origin, float radiusSqr)
    {
        var players = FindObjectsByType<CwslPlayerHealth>(FindObjectsSortMode.None);
        foreach (var health in players)
        {
            if (health == null || health.transform == transform)
                continue;

            var visible = IsInRadius(origin, health.transform.position, radiusSqr);
            SetOccludeeVisible(health.gameObject, visible);
        }
    }

    private static bool IsInRadius(Vector3 origin, Vector3 worldPosition, float radiusSqr)
    {
        var flat = worldPosition - origin;
        flat.y = 0f;
        return flat.sqrMagnitude <= radiusSqr;
    }

    private static void SetOccludeeVisible(GameObject target, bool visible)
    {
        var occludee = target.GetComponent<CwslVisionOccludee>();
        if (occludee == null)
            occludee = target.AddComponent<CwslVisionOccludee>();
        occludee.SetVisible(visible);
    }
}
