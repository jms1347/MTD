using UnityEngine;

public class CwslMoveDestinationMarker : MonoBehaviour
{
    private const float Lifetime = 2.2f;
    private const float PulseSpeed = 5f;

    private static CwslMoveDestinationMarker instance;

    private Transform ring;
    private Transform core;
    private float hideTimer;
    private Renderer ringRenderer;
    private Renderer coreRenderer;
    private Color ringBaseColor;
    private Color coreBaseColor;

    public static void Show(Vector3 worldPoint)
    {
        EnsureInstance();
        instance.ShowAt(worldPoint, isAttackMove: false);
    }

    public static void ShowAttack(Vector3 worldPoint)
    {
        EnsureInstance();
        instance.ShowAt(worldPoint, isAttackMove: true);
    }

    private static void EnsureInstance()
    {
        if (instance != null)
            return;

        var root = new GameObject("CwslMoveDestinationMarker");
        instance = root.AddComponent<CwslMoveDestinationMarker>();
        instance.BuildVisual();
        DontDestroyOnLoad(root);
    }

    private void BuildVisual()
    {
        var root = new GameObject("MarkerRoot");
        root.transform.SetParent(transform, false);

        core = GameObject.CreatePrimitive(PrimitiveType.Cylinder).transform;
        core.name = "Core";
        core.SetParent(root.transform, false);
        core.localScale = new Vector3(0.55f, 0.03f, 0.55f);
        core.localPosition = new Vector3(0f, 0.04f, 0f);
        Object.Destroy(core.GetComponent<Collider>());
        coreRenderer = core.GetComponent<Renderer>();

        ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder).transform;
        ring.name = "Ring";
        ring.SetParent(root.transform, false);
        ring.localScale = new Vector3(1.15f, 0.02f, 1.15f);
        ring.localPosition = new Vector3(0f, 0.03f, 0f);
        Object.Destroy(ring.GetComponent<Collider>());
        ringRenderer = ring.GetComponent<Renderer>();

        ApplyColors(isAttackMove: false);
        gameObject.SetActive(false);
    }

    private void ShowAt(Vector3 worldPoint, bool isAttackMove)
    {
        ApplyColors(isAttackMove);
        transform.position = worldPoint;
        hideTimer = Lifetime;
        gameObject.SetActive(true);
    }

    private void ApplyColors(bool isAttackMove)
    {
        if (isAttackMove)
        {
            coreBaseColor = new Color(1f, 0.2f, 0.12f, 0.95f);
            ringBaseColor = new Color(1f, 0.15f, 0.1f, 0.7f);
        }
        else
        {
            coreBaseColor = new Color(0.35f, 0.95f, 0.55f, 0.85f);
            ringBaseColor = new Color(0.2f, 0.85f, 0.95f, 0.55f);
        }

        CwslMaterialUtil.ApplyColor(coreRenderer, coreBaseColor);
        CwslMaterialUtil.ApplyColor(ringRenderer, ringBaseColor);
    }

    private void Update()
    {
        if (!gameObject.activeSelf)
            return;

        hideTimer -= Time.deltaTime;
        if (hideTimer <= 0f)
        {
            gameObject.SetActive(false);
            return;
        }

        var pulse = 0.85f + Mathf.Sin(Time.time * PulseSpeed) * 0.15f;
        ring.localScale = new Vector3(1.15f * pulse, 0.02f, 1.15f * pulse);

        var alpha = Mathf.Clamp01(hideTimer / Lifetime);
        SetAlpha(ringRenderer, ringBaseColor, alpha);
        SetAlpha(coreRenderer, coreBaseColor, alpha);
    }

    private static void SetAlpha(Renderer renderer, Color baseColor, float alpha)
    {
        if (renderer == null || renderer.material == null)
            return;

        var color = baseColor;
        color.a = baseColor.a * alpha;
        renderer.material.color = color;
        if (renderer.material.HasProperty("_BaseColor"))
            renderer.material.SetColor("_BaseColor", color);
        if (renderer.material.HasProperty("_Color"))
            renderer.material.SetColor("_Color", color);
    }
}
