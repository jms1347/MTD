using UnityEngine;

/// <summary>
/// 타워 시트 ID → 비주얼 프리팹/프로시저럴 생성 (배치·건설 미리보기 공통).
/// </summary>
public static class DefenseTowerVisualResolver
{
    public const float DefaultVisualScale = 1.2f;

    public static bool TryInstantiateVisual(
        Transform root,
        TowerSpawnData data,
        int sheetTowerId,
        TowerKind kind)
    {
        if (root == null)
            return false;

        if (TryInstantiateAddressableVisual(root, sheetTowerId))
            return true;

        DefenseTowerVisualBuilder.Build(root, data, sheetTowerId, kind);
        StripColliders(root);
        return true;
    }

    public static bool TryInstantiateAddressableVisual(Transform root, int sheetTowerId)
    {
        if (sheetTowerId <= 0 || DataManager.Instance == null)
            return false;

        if (!DataManager.Instance.TryGetTower(sheetTowerId, out var towerData))
            return false;

        var key = towerData.ResolvePrefabKey();
        if (string.IsNullOrWhiteSpace(key))
            return false;

        if (!DefenseAddressableLoader.TryLoadPrefab(key, out var prefab) || prefab == null)
            return false;

        if (!HasUsableMaterials(prefab))
            return false;

        var instance = Object.Instantiate(prefab, root);
        instance.name = "Visual";
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;

        if (!HasUsableMaterials(instance))
        {
            DestroySafe(instance);
            return false;
        }

        StripColliders(root);
        return true;
    }

    public static bool HasUsableMaterials(GameObject target)
    {
        if (target == null)
            return false;

        var renderers = target.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return false;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null || renderers[i].sharedMaterial == null)
                return false;
        }

        return true;
    }

    public static void StripColliders(Transform root)
    {
        if (root == null)
            return;

        foreach (var collider in root.GetComponentsInChildren<Collider>())
            DestroySafe(collider);
    }

    private static void DestroySafe(Object obj)
    {
        if (obj == null)
            return;

        if (Application.isPlaying)
            Object.Destroy(obj);
        else
            Object.DestroyImmediate(obj);
    }
}
