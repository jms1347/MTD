using UnityEngine;

/// <summary>
/// Player / HumanTarget 레이어 + 카메라 컬링 마스크 유틸.
/// 인간 카메라: HumanTarget 제외 / 모기 카메라: HumanTarget 포함.
/// </summary>
public static class PanicVisionLayers
{
    public const string PlayerLayerName = "Player";
    public const string HumanTargetLayerName = "HumanTarget";

    public static int PlayerLayer
    {
        get
        {
            var layer = LayerMask.NameToLayer(PlayerLayerName);
            return layer >= 0 ? layer : 0;
        }
    }

    public static int HumanTargetLayer
    {
        get
        {
            var layer = LayerMask.NameToLayer(HumanTargetLayerName);
            return layer >= 0 ? layer : 0;
        }
    }

    public static int PlayerMask => 1 << PlayerLayer;
    public static int HumanTargetMask => 1 << HumanTargetLayer;

    public static void ApplyHumanCameraCulling(Camera camera)
    {
        if (camera == null)
            return;

        EnsureLayersExist();
        camera.cullingMask |= PlayerMask;
        camera.cullingMask &= ~HumanTargetMask;
    }

    public static void ApplyMosquitoCameraCulling(Camera camera)
    {
        if (camera == null)
            return;

        EnsureLayersExist();
        camera.cullingMask |= PlayerMask;
        camera.cullingMask |= HumanTargetMask;
    }

    public static void SetLayerRecursive(GameObject root, int layer)
    {
        if (root == null)
            return;

        root.layer = layer;
        for (var i = 0; i < root.transform.childCount; i++)
            SetLayerRecursive(root.transform.GetChild(i).gameObject, layer);
    }

    public static void EnsureLayerExists() => EnsureLayersExist();

#if UNITY_EDITOR
    public static void EnsureLayersExist()
    {
        EnsureNamedLayer(PlayerLayerName);
        EnsureNamedLayer(HumanTargetLayerName);
    }

    private static void EnsureNamedLayer(string layerName)
    {
        if (LayerMask.NameToLayer(layerName) >= 0)
            return;

        var tagManager = new UnityEditor.SerializedObject(
            UnityEditor.AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tagManager.FindProperty("layers");
        for (var i = 8; i < layers.arraySize; i++)
        {
            var slot = layers.GetArrayElementAtIndex(i);
            if (!string.IsNullOrEmpty(slot.stringValue))
                continue;

            slot.stringValue = layerName;
            tagManager.ApplyModifiedProperties();
            Debug.Log($"[PanicVision] Created layer: {layerName} (index {i})");
            return;
        }

        Debug.LogError($"[PanicVision] Free layer slot not found for {layerName}.");
    }
#else
    public static void EnsureLayersExist()
    {
        if (LayerMask.NameToLayer(PlayerLayerName) < 0)
            Debug.LogWarning("[PanicVision] Player layer missing. Add it in Tag Manager.");
        if (LayerMask.NameToLayer(HumanTargetLayerName) < 0)
            Debug.LogWarning("[PanicVision] HumanTarget layer missing. Add it in Tag Manager.");
    }
#endif
}
