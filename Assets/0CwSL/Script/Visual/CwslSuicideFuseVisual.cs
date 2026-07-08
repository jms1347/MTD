using UnityEngine;

/// <summary>심지 끝 BombFuse 파티클 — 근접 시에만 생성·재생.</summary>
public class CwslSuicideFuseVisual : MonoBehaviour
{
    [SerializeField] private float scale = 0.2f;
    private GameObject instance;

    public static void Ensure(Transform fuseTip, float fuseScale = 0.2f)
    {
        if (fuseTip == null)
            return;

        var visual = fuseTip.GetComponent<CwslSuicideFuseVisual>();
        if (visual == null)
            visual = fuseTip.gameObject.AddComponent<CwslSuicideFuseVisual>();

        visual.scale = fuseScale;
        if (!Application.isPlaying)
            visual.Refresh();
    }

    private void Awake()
    {
        if (Application.isPlaying)
            DestroyEmbeddedFuseChildren();
    }

    private void OnDestroy()
    {
        DestroyInstance();
    }

    public void Refresh()
    {
        if (instance != null)
            return;

        var prefab = ResolvePrefab();
        if (prefab == null)
            return;

        instance = Instantiate(prefab, transform);
        instance.name = "BombFuseFx";
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one * scale;
    }

    public void SetBurningActive(bool active)
    {
        if (!isActiveAndEnabled)
            return;

        if (!active)
        {
            DestroyInstance();
            DestroyEmbeddedFuseChildren();
            return;
        }

        if (instance == null)
            Refresh();
        else if (!instance.activeSelf)
            instance.SetActive(true);
    }

    private void DestroyEmbeddedFuseChildren()
    {
        if (this == null || transform == null)
            return;

        for (var i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            if (child.name != "BombFuseFx")
                continue;

            if (child.gameObject == instance)
                continue;

            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
    }

    private void DestroyInstance()
    {
        if (instance == null)
            return;

        if (Application.isPlaying)
            Destroy(instance);
        else
            DestroyImmediate(instance);

        instance = null;
    }

    private static GameObject ResolvePrefab()
    {
        var assets = CwslGameSession.Instance?.Assets;
        if (assets != null && assets.bombFuseVfx != null)
            return assets.bombFuseVfx;

#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(CwslVfxPaths.BombFuse);
#else
        return null;
#endif
    }
}
