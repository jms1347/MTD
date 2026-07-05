using UnityEngine;

/// <summary>
/// 풀링/프리팹에서 머티리얼이 비어 있을 때 골드 코인 색을 복구한다.
/// </summary>
public class CwslGoldCoinMaterialFix : MonoBehaviour
{
    private static readonly Color GoldColor = new(1f, 0.84f, 0.12f);

    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        var renderers = GetComponentsInChildren<Renderer>(true);
        for (var i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
                continue;

            if (renderers[i].sharedMaterial != null && CwslMaterialUtil.IsMaterialValid(renderers[i].sharedMaterial))
                continue;

            CwslMaterialUtil.ApplyColor(renderers[i], GoldColor);
        }
    }
}
