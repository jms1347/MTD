using UnityEngine;

/// <summary>비주얼 테스트 씬 — CwslGameSession 없이 VFX 에셋을 초기화합니다.</summary>
public class CwslVisualTestAssetsBootstrap : MonoBehaviour
{
    [SerializeField] private CwslGameAssets assets;

    private void Awake()
    {
        if (assets == null)
        {
#if UNITY_EDITOR
            assets = UnityEditor.AssetDatabase.LoadAssetAtPath<CwslGameAssets>("Assets/0CwSL/Data/CwslGameAssets.asset");
#endif
        }

        if (assets != null)
            CwslVisualTestAssetsContext.Set(assets);
    }
}
