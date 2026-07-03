using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 시야 경계를 smoothstep으로 부드럽게 페이드.
/// 시야 없는 캐릭터는 적/미사일이 거의 자기 발밑까지 와야 보임.
/// </summary>
public class CwslLocalVisionSystem : MonoBehaviour
{
    private const float RefreshInterval = 0.05f;

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
        var radius = playerVision.EffectiveVisionRadius;
        var blind = playerVision.VisionRadius <= 0.01f;

        RefreshMonsters(origin, radius, blind);
        RefreshGold(origin, radius, blind);
        RefreshOtherPlayers(origin, radius, blind);
        RefreshProjectiles(origin, radius, blind);
    }

    private void RefreshMonsters(Vector3 origin, float radius, bool blind)
    {
        var monsters = FindObjectsByType<CwslMonsterBase>(FindObjectsSortMode.None);
        foreach (var monster in monsters)
        {
            if (monster == null)
                continue;

            var health = monster.GetComponent<CwslMonsterHealth>();
            if (health != null && !health.IsAlive)
            {
                SetOccludeeVisibility(monster.gameObject, 0f);
                continue;
            }

            var visibility = EvaluateVisibility(origin, monster.transform.position, radius, blind, isProjectile: false);
            SetOccludeeVisibility(monster.gameObject, visibility);
        }
    }

    private void RefreshGold(Vector3 origin, float radius, bool blind)
    {
        var pickups = FindObjectsByType<CwslGoldPickup>(FindObjectsSortMode.None);
        foreach (var pickup in pickups)
        {
            if (pickup == null)
                continue;

            var visibility = EvaluateVisibility(origin, pickup.transform.position, radius, blind, isProjectile: false);
            SetOccludeeVisibility(pickup.gameObject, visibility);
        }
    }

    private void RefreshOtherPlayers(Vector3 origin, float radius, bool blind)
    {
        var players = FindObjectsByType<CwslPlayerHealth>(FindObjectsSortMode.None);
        foreach (var health in players)
        {
            if (health == null || health.transform == transform)
                continue;

            var visibility = EvaluateVisibility(origin, health.transform.position, radius, blind, isProjectile: false);
            SetOccludeeVisibility(health.gameObject, visibility);
        }
    }

    private void RefreshProjectiles(Vector3 origin, float radius, bool blind)
    {
        // 적 미사일: 시야 밖/경계에서는 거의 안 보여서 피격 원인을 알기 어렵게
        var projectiles = FindObjectsByType<CwslMonsterProjectile>(FindObjectsSortMode.None);
        foreach (var projectile in projectiles)
        {
            if (projectile == null)
                continue;

            var visibility = EvaluateVisibility(origin, projectile.transform.position, radius, blind, isProjectile: true);
            SetOccludeeVisibility(projectile.gameObject, visibility);
        }
    }

    /// <summary>
    /// smoothstep 기반 시야 페이드.
    /// blind(시야 0): 거의 자기만, 미사일은 더 엄격.
    /// 일반: 안쪽 선명 → 경계 은은한 실루엣 → 바깥 소멸.
    /// </summary>
    public static float EvaluateVisibility(
        Vector3 origin,
        Vector3 worldPosition,
        float radius,
        bool blind,
        bool isProjectile)
    {
        var flat = worldPosition - origin;
        flat.y = 0f;
        var distance = flat.magnitude;

        if (blind)
        {
            // 시야 없는 캐릭터: 완전 가깝지 않으면 거의 안 보임
            if (isProjectile)
            {
                var hideAt = radius * 0.55f;
                if (distance >= hideAt)
                    return 0f;
                var t = Mathf.SmoothStep(0f, 1f, distance / hideAt);
                return Mathf.Lerp(0.35f, 0f, t);
            }

            var inner = radius * 0.2f;
            var outer = radius * 0.85f;
            if (distance <= inner)
                return 1f;
            if (distance >= outer)
                return 0f;

            var fade = Mathf.SmoothStep(0f, 1f, (distance - inner) / Mathf.Max(0.01f, outer - inner));
            return Mathf.Lerp(1f, 0f, fade);
        }

        // 일반 시야: 부드러운 그라데이션 + 바깥 은은한 실루엣
        if (isProjectile)
        {
            var inner = radius * 0.45f;
            var outer = radius * 1.05f;
            if (distance <= inner)
                return 1f;
            if (distance >= outer)
                return 0f;
            var t = Mathf.SmoothStep(0f, 1f, (distance - inner) / (outer - inner));
            return Mathf.Lerp(1f, 0f, t);
        }

        var fullInner = radius * 0.45f;
        var softEdge = radius * 0.95f;
        var silhouetteOuter = radius * 1.45f;

        if (distance <= fullInner)
            return 1f;
        if (distance >= silhouetteOuter)
            return 0f;

        if (distance <= softEdge)
        {
            var t = Mathf.SmoothStep(0f, 1f, (distance - fullInner) / Mathf.Max(0.01f, softEdge - fullInner));
            return Mathf.Lerp(1f, 0.18f, t);
        }

        var tOuter = Mathf.SmoothStep(0f, 1f, (distance - softEdge) / Mathf.Max(0.01f, silhouetteOuter - softEdge));
        return Mathf.Lerp(0.18f, 0f, tOuter);
    }

    private static void SetOccludeeVisibility(GameObject target, float visibility)
    {
        var occludee = target.GetComponent<CwslVisionOccludee>();
        if (occludee == null)
            occludee = target.AddComponent<CwslVisionOccludee>();
        occludee.SetVisibility(visibility);
    }
}
