using UnityEngine;

/// <summary>
/// 즉시타격 스킬의 착탄·총구 연출 — 미사일 비행 없이 타격 지점에 이펙트만 재생.
/// </summary>
public static class DefenseInstantHitVfx
{
    private struct Entry
    {
        public string muzzleKey;
        public string impactKey;
        public bool groundImpact;
    }

    public static void PlayStrike(DefenseSkillData skill, Vector3 origin, Vector3 hitPoint)
    {
        if (skill == null)
            return;

        if (!TryGetEntry(skill.skillCode, out var entry))
        {
            PlayFallbackImpact(hitPoint);
            return;
        }

        if (!string.IsNullOrWhiteSpace(entry.muzzleKey))
            TrySpawnAt(origin, Quaternion.identity, entry.muzzleKey, 0.35f);

        if (!string.IsNullOrWhiteSpace(entry.impactKey))
        {
            if (entry.groundImpact)
                DefenseCombatVfxSpawn.TrySpawnGroundBurst(entry.impactKey, hitPoint, 2.2f);
            else
                TrySpawnAt(hitPoint, Quaternion.identity, entry.impactKey, 1.6f);
        }
    }

    private static bool TryGetEntry(string skillCode, out Entry entry)
    {
        entry = default;
        if (string.IsNullOrWhiteSpace(skillCode))
            return false;

        switch (skillCode.Trim().ToUpperInvariant())
        {
            case "M-N-0005":
                entry = new Entry
                {
                    muzzleKey = "GunFireYellow",
                    impactKey = "BulletFatExplosionPink",
                    groundImpact = false
                };
                return true;
            default:
                return false;
        }
    }

    private static void PlayFallbackImpact(Vector3 hitPoint)
    {
        DefenseCombatVfxSpawn.TrySpawnGroundBurst("BulletFatExplosionPink", hitPoint, 1.2f);
    }

    private static void TrySpawnAt(Vector3 position, Quaternion rotation, string key, float lifetime)
    {
        if (!DefenseCombatVfxSpawn.TryLoadBurstPrefab(key, out var prefab) || prefab == null)
            return;

        var fx = Object.Instantiate(prefab, position, rotation);
        Object.Destroy(fx, lifetime);
    }
}
