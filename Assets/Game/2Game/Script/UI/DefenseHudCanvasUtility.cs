using UnityEngine;

/// <summary>
/// DefenseHUD 루트(자식 Canvas 구조)에서 UI용 Canvas를 찾습니다.
/// </summary>
public static class DefenseHudCanvasUtility
{
    public static Canvas ResolveCanvas(Transform hudRoot)
    {
        if (hudRoot == null)
            return null;

        var canvas = hudRoot.GetComponentInChildren<Canvas>(true);
        if (canvas != null)
            return canvas;

        canvas = hudRoot.GetComponent<Canvas>();
        if (canvas != null)
            return canvas;

        return hudRoot.GetComponentInParent<Canvas>();
    }
}
