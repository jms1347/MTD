using UnityEngine;

/// <summary>심지 끝 BombFuse 파티클 (에디터 프리팹 미리보기 + 런타임).</summary>
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
        visual.Refresh();
    }

    private void OnEnable()
    {
        Refresh();
    }

    private void OnDestroy()
    {
        if (instance == null)
            return;

        if (Application.isPlaying)
            Destroy(instance);
        else
            DestroyImmediate(instance);
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
        if (instance == null)
            Refresh();

        if (instance == null)
            return;

        instance.SetActive(active);
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
