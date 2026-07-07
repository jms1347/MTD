using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 끌모 차지 중 영역 안 적·투사체에 슬로우 VFX 부착 (클라이언트 전용).
/// </summary>
public static class CwslGatherSlowVisual
{
    private static readonly Dictionary<int, GameObject> vfxByTargetId = new();

    public static void Sync(Vector3 center, float radius)
    {
        var activeIds = new HashSet<int>();
        var radiusSqr = radius * radius;

        TrackMonsters(center, radiusSqr, activeIds);
        TrackProjectiles<CwslMonsterProjectile>(center, radiusSqr, activeIds);
        TrackProjectiles<CwslPlayerProjectile>(center, radiusSqr, activeIds);

        RemoveStale(activeIds);
    }

    public static void Clear()
    {
        foreach (var pair in vfxByTargetId)
        {
            if (pair.Value != null)
                Object.Destroy(pair.Value);
        }

        vfxByTargetId.Clear();
    }

    private static void TrackMonsters(Vector3 center, float radiusSqr, HashSet<int> activeIds)
    {
        // 몬스터 동상 VFX는 CwslMonsterStatusVfx(네트워크 동기화)가 담당.
    }

    private static void TrackProjectiles<T>(Vector3 center, float radiusSqr, HashSet<int> activeIds)
        where T : Component
    {
        var projectiles = Object.FindObjectsByType<T>(FindObjectsSortMode.None);
        foreach (var projectile in projectiles)
        {
            if (projectile == null)
                continue;

            if (!IsInsideFlatRadius(center, projectile.transform.position, radiusSqr))
                continue;

            EnsureVfx(projectile.transform, activeIds);
        }
    }

    private static void EnsureVfx(Transform target, HashSet<int> activeIds)
    {
        var id = target.GetInstanceID();
        activeIds.Add(id);

        if (vfxByTargetId.TryGetValue(id, out var existing) && existing != null)
            return;

        if (existing != null)
            Object.Destroy(existing);

        var anchor = target.Find("Visual") ?? target;
        vfxByTargetId[id] = CwslVfxSpawner.AttachGatherSlowEnchant(anchor);
    }

    private static void RemoveStale(HashSet<int> activeIds)
    {
        var stale = new List<int>();
        foreach (var pair in vfxByTargetId)
        {
            if (activeIds.Contains(pair.Key))
                continue;

            stale.Add(pair.Key);
            if (pair.Value != null)
                Object.Destroy(pair.Value);
        }

        foreach (var id in stale)
            vfxByTargetId.Remove(id);
    }

    private static bool IsInsideFlatRadius(Vector3 center, Vector3 target, float radiusSqr)
    {
        var flat = target - center;
        flat.y = 0f;
        return flat.sqrMagnitude <= radiusSqr;
    }
}
