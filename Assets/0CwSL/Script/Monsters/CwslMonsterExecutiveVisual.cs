using UnityEngine;

public static class CwslMonsterExecutiveVisual
{
    public static void Apply(Transform root)
    {
        if (root == null)
            return;

        var visual = root.Find("Visual");
        if (visual == null)
            visual = root;

        visual.localScale = Vector3.one * 1.35f;
        foreach (var renderer in visual.GetComponentsInChildren<Renderer>())
            CwslMaterialUtil.ApplyColor(renderer, new Color(0.95f, 0.78f, 0.12f));
    }
}
