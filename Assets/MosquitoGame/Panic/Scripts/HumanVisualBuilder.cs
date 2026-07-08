using UnityEngine;

/// <summary>
/// Human(Player) 바디 + HumanTarget 아웃라인 이중 메쉬 빌더.
/// Body: 인간/모기 모두에게 보이는 일반 메쉬 (Player 레이어 + Collider)
/// Outline: 모기만 보는 빨간 투시 껍데기 (HumanTarget 레이어, Collider 없음)
/// </summary>
public class HumanVisualBuilder : MonoBehaviour
{
    [SerializeField] private Transform bodyRoot;
    [SerializeField] private Transform outlineRoot;
    [SerializeField] private Collider bodyCollider;
    [SerializeField] private bool outlineThroughWalls = true;

    public Transform BodyRoot => bodyRoot;
    public Transform OutlineRoot => outlineRoot;
    public Collider BodyCollider => bodyCollider;

    public void Build()
    {
        if (bodyRoot != null && outlineRoot != null)
            return;

        PanicVisionLayers.EnsureLayersExist();

        BuildBody();
        BuildOutlineShell();
    }

    private void BuildBody()
    {
        if (bodyRoot != null)
            return;

        var root = new GameObject("BodyMesh");
        root.transform.SetParent(transform, false);
        root.transform.localPosition = new Vector3(0f, 0.95f, 0f);

        var torso = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        torso.name = "Torso";
        torso.transform.SetParent(root.transform, false);
        torso.transform.localPosition = Vector3.zero;
        torso.transform.localScale = new Vector3(0.55f, 0.9f, 0.55f);
        PanicMaterialFactory.ApplyColor(torso.GetComponent<Renderer>(), new Color(0.28f, 0.55f, 0.95f));

        var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        head.transform.SetParent(root.transform, false);
        head.transform.localPosition = new Vector3(0f, 1.05f, 0f);
        head.transform.localScale = Vector3.one * 0.42f;
        PanicMaterialFactory.ApplyColor(head.GetComponent<Renderer>(), new Color(0.95f, 0.82f, 0.7f));
        Destroy(head.GetComponent<Collider>());

        // 충돌은 몸통 Capsule 하나로 통일 (Player 레이어)
        bodyCollider = torso.GetComponent<CapsuleCollider>();
        PanicVisionLayers.SetLayerRecursive(root, PanicVisionLayers.PlayerLayer);
        bodyRoot = root.transform;
    }

    private void BuildOutlineShell()
    {
        if (outlineRoot != null)
            return;

        var root = new GameObject("OutlineMesh");
        root.transform.SetParent(transform, false);
        root.transform.localPosition = new Vector3(0f, 0.95f, 0f);

        // 바디와 동일 형태를 약간 키워 겹침
        var torso = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        torso.name = "OutlineTorso";
        torso.transform.SetParent(root.transform, false);
        torso.transform.localPosition = Vector3.zero;
        torso.transform.localScale = new Vector3(0.62f, 0.98f, 0.62f);
        PanicMaterialFactory.ApplyColor(
            torso.GetComponent<Renderer>(),
            new Color(1f, 0.08f, 0.08f, 0.45f),
            transparent: true);
        Destroy(torso.GetComponent<Collider>());

        var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "OutlineHead";
        head.transform.SetParent(root.transform, false);
        head.transform.localPosition = new Vector3(0f, 1.05f, 0f);
        head.transform.localScale = Vector3.one * 0.52f;
        PanicMaterialFactory.ApplyColor(
            head.GetComponent<Renderer>(),
            new Color(1f, 0.12f, 0.12f, 0.55f),
            transparent: true);
        Destroy(head.GetComponent<Collider>());

        if (outlineThroughWalls)
            ApplyXRayMaterials(root);

        PanicVisionLayers.SetLayerRecursive(root, PanicVisionLayers.HumanTargetLayer);
        outlineRoot = root.transform;
    }

    private static void ApplyXRayMaterials(GameObject root)
    {
        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null || renderer.sharedMaterial == null)
                continue;

            var material = new Material(renderer.sharedMaterial);
            material.SetInt("_ZWrite", 0);
            material.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
            material.renderQueue = 4000;
            renderer.sharedMaterial = material;
        }
    }

    public void SetOutlineVisible(bool visible)
    {
        if (outlineRoot != null)
            outlineRoot.gameObject.SetActive(visible);
    }

    public Vector3 GetNearestAttachPoint(Vector3 fromPosition)
    {
        if (bodyCollider != null)
            return bodyCollider.ClosestPoint(fromPosition);

        return transform.position + Vector3.up * 1.1f;
    }
}
