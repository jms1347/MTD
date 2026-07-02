using UnityEngine;

/// <summary>
/// 타워 발사 시 화염방사·레이저·독 분사 등 지속 파티클 연출.
/// </summary>
public static class DefenseTowerSkillVfx
{
    public static bool TryCreatePresentationInstance(
        Transform parent,
        DefenseSkillPresentationType type,
        string effectKey,
        out GameObject instance)
    {
        instance = null;
        if (parent == null || !TryLoadPresentationPrefab(effectKey, out var prefab) || prefab == null)
            return false;

        instance = Object.Instantiate(prefab, parent);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        ApplyPresentationScale(instance.transform, type);
        instance.SetActive(false);
        return true;
    }

    /// <summary>
    /// 분사 파티클 lifetime×speed 기준으로 눈에 보이는 도달 거리를 추정합니다.
    /// </summary>
    public static bool TryEstimatePresentationReach(
        string effectKey,
        DefenseSkillPresentationType type,
        out float reach)
    {
        reach = 0f;
        if (!TryLoadPresentationPrefab(effectKey, out var prefab) || prefab == null)
            return false;

        float maxRawTravel = 0f;
        var particleSystems = prefab.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particleSystems.Length; i++)
        {
            var main = particleSystems[i].main;
            float lifetime = ReadCurveMax(main.startLifetime);
            float speed = ReadCurveMax(main.startSpeed);
            maxRawTravel = Mathf.Max(maxRawTravel, lifetime * speed);
        }

        if (maxRawTravel <= 0.01f)
            return false;

        float scale = type switch
        {
            DefenseSkillPresentationType.SustainedFlamethrower => 1.1f,
            DefenseSkillPresentationType.SustainedSpray => 1f,
            _ => 1f
        };

        float visibleFactor = type switch
        {
            DefenseSkillPresentationType.SustainedFlamethrower => 0.55f,
            DefenseSkillPresentationType.SustainedSpray => 0.35f,
            _ => 0.45f
        };

        reach = Mathf.Clamp(maxRawTravel * scale * visibleFactor, 5f, 12f);
        return true;
    }

    private static float ReadCurveMax(ParticleSystem.MinMaxCurve curve)
    {
        return curve.mode switch
        {
            ParticleSystemCurveMode.TwoConstants => curve.constantMax,
            ParticleSystemCurveMode.TwoCurves => curve.constantMax,
            _ => curve.constant
        };
    }

    private static void ApplyPresentationScale(Transform root, DefenseSkillPresentationType type)
    {
        switch (type)
        {
            case DefenseSkillPresentationType.SustainedLaser:
                root.localScale = Vector3.one * 0.85f;
                break;
            case DefenseSkillPresentationType.SustainedFlamethrower:
                root.localScale = Vector3.one * 1.1f;
                break;
            case DefenseSkillPresentationType.SustainedSpray:
                root.localScale = Vector3.one * 1f;
                break;
        }
    }

    private static bool TryLoadPresentationPrefab(string key, out GameObject prefab)
    {
        prefab = null;
        if (string.IsNullOrWhiteSpace(key))
            return false;

        if (DefenseAddressableLoader.TryLoadEffect(key, out prefab) && prefab != null)
            return true;

        if (DefenseAddressableLoader.TryLoadMissile(key, out prefab) && prefab != null)
            return true;

        return DefenseAddressableLoader.TryLoadPrefab(key, out prefab) && prefab != null;
    }
}
