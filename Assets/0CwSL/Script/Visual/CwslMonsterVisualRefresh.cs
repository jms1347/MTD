using UnityEngine;

public static class CwslMonsterVisualRefresh
{
    public static void Refresh(Transform root, CwslMonsterType type)
    {
        if (root == null)
            return;

        var existing = root.Find("Visual");
        if (existing != null)
        {
            if (Application.isPlaying)
                Object.Destroy(existing.gameObject);
            else
                Object.DestroyImmediate(existing.gameObject);
        }

        CwslMonsterVisualBuilder.Build(root, type);
    }
}
