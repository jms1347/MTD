using UnityEngine;

/// <summary>프리팹/풀에서 머티리얼이 비었을 때 저장된 색·재질을 복구한다.</summary>
[ExecuteAlways]
public class CwslColoredRenderer : MonoBehaviour
{
    [SerializeField] private Color color = Color.white;
    [SerializeField] private CwslMaterialStyle style = CwslMaterialStyle.Matte;

    public Color StoredColor => color;
    public CwslMaterialStyle StoredStyle => style;

    public void SetColor(Color value, CwslMaterialStyle materialStyle = CwslMaterialStyle.Matte)
    {
        color = value;
        style = materialStyle;
        Apply(force: true);
    }

    private void OnEnable()
    {
        Apply();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        Apply(force: true);
    }
#endif

    private void Apply(bool force = false)
    {
        var renderer = GetComponent<Renderer>();
        if (renderer == null)
            return;

        if (!force && renderer.sharedMaterial != null && CwslMaterialUtil.IsMaterialValid(renderer.sharedMaterial))
            return;

        CwslMaterialUtil.ApplyStyled(renderer, color, style);
    }
}
