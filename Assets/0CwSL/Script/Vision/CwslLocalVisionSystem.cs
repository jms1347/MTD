using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
/// <summary>
/// 시야 경계를 smoothstep으로 부드럽게 페이드.
/// 내 캐릭터 시야만 적용 (화면·오클루전 일치).
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
        if (playerVision == null)
            return;

        RefreshAlliedStructures();
        RefreshMonsters();
        RefreshGold();
        RefreshPills();
        RefreshOtherPlayers();
        RefreshProjectiles();
    }

    private void RefreshAlliedStructures()
    {
        var nexus = CwslNexus.Instance;
        if (nexus != null && nexus.IsAlive)
            SetOccludeeVisibility(nexus.gameObject, 1f);
    }

    private void RefreshMonsters()
    {
        var monsters = CwslCombatRegistry.AliveMonsters;
        foreach (var health in monsters)
        {
            if (health == null)
                continue;

            var monster = health.GetComponent<CwslMonsterBase>();
            if (monster == null)
                continue;

            if (!health.IsAlive)
            {
                SetOccludeeVisibility(monster.gameObject, 0f);
                continue;
            }

            var visibility = playerVision.EvaluateLocalVisibility(monster.transform.position);
            SetOccludeeVisibility(monster.gameObject, visibility);
        }
    }

    private void RefreshGold()
    {
        var pickups = CwslCombatRegistry.ActiveGoldPickups;
        foreach (var pickup in pickups)
        {
            if (pickup == null)
                continue;

            var visibility = playerVision.EvaluateLocalVisibility(pickup.transform.position);
            SetOccludeeVisibility(pickup.gameObject, visibility);
        }
    }

    private void RefreshPills()
    {
        var pickups = CwslCombatRegistry.ActivePillPickups;
        foreach (var pickup in pickups)
        {
            if (pickup == null)
                continue;

            var visibility = playerVision.EvaluateLocalVisibility(pickup.transform.position);
            SetOccludeeVisibility(pickup.gameObject, visibility);
        }
    }

    private void RefreshOtherPlayers()
    {
        var players = CwslCombatRegistry.AlivePlayers;
        foreach (var health in players)
        {
            if (health == null || health.transform == transform)
                continue;

            var visibility = playerVision.EvaluateLocalVisibility(health.transform.position);
            SetOccludeeVisibility(health.gameObject, visibility);
        }
    }

    private void RefreshProjectiles()
    {
        var projectiles = CwslCombatRegistry.ActiveMonsterProjectiles;
        foreach (var projectile in projectiles)
        {
            if (projectile == null)
                continue;

            var visibility = playerVision.EvaluateLocalVisibility(projectile.transform.position, isProjectile: true);
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
        var distance = CwslVisionShape.GetCircularFlatDistance(origin, worldPosition);

        if (CwslArenaGimmickSystem.IsBossFinalPhaseDarkness
            && !CwslArenaGimmickSystem.IsInsideFinalPhaseVision(worldPosition))
            return 0f;

        if (blind)
        {
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
