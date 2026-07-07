using UnityEngine;

/// <summary>수석 코치 철밥통 쉴드 — 자폭 몬스터 1회 피격 무시 연출.</summary>
public class CwslMonsterIronPotShieldVisual : MonoBehaviour
{
    private GameObject shieldRoot;
    private Renderer shieldRenderer;

    public static CwslMonsterIronPotShieldVisual Ensure(GameObject root)
    {
        if (root == null)
            return null;

        var visual = root.GetComponent<CwslMonsterIronPotShieldVisual>();
        if (visual == null)
            visual = root.AddComponent<CwslMonsterIronPotShieldVisual>();

        return visual;
    }

    public void ShowShield()
    {
        EnsureShield();
        if (shieldRoot != null)
            shieldRoot.SetActive(true);

        SetColor(new Color(0.72f, 0.78f, 0.88f, 0.42f));
    }

    public void PulseBlocked()
    {
        EnsureShield();
        SetColor(new Color(0.95f, 0.98f, 1f, 0.72f));
    }

    public void HideShield()
    {
        if (shieldRoot != null)
            shieldRoot.SetActive(false);
    }

    private void SetColor(Color color)
    {
        if (shieldRenderer == null)
            return;

        var block = new MaterialPropertyBlock();
        shieldRenderer.GetPropertyBlock(block);
        block.SetColor("_Color", color);
        shieldRenderer.SetPropertyBlock(block);
    }

    private void EnsureShield()
    {
        if (shieldRoot != null)
            return;

        shieldRoot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        shieldRoot.name = "IronPotShield";
        shieldRoot.transform.SetParent(transform, false);
        shieldRoot.transform.localPosition = new Vector3(0f, 0.95f, 0f);
        shieldRoot.transform.localScale = Vector3.one * 1.55f;

        var collider = shieldRoot.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);

        shieldRenderer = shieldRoot.GetComponent<Renderer>();
        if (shieldRenderer != null)
        {
            var mat = shieldRenderer.material;
            mat.color = new Color(0.72f, 0.78f, 0.88f, 0.42f);
        }

        shieldRoot.SetActive(false);
    }
}
