using UnityEngine;

/// <summary>주황색 스폰 링의 적 기지 마커 (비주얼).</summary>
public class CwslEnemyBaseMarker : MonoBehaviour
{
    private static readonly Color BaseColor = new(1f, 0.45f, 0.08f, 0.92f);

    public Vector3 SpawnPosition { get; private set; }

    public static CwslEnemyBaseMarker Create(Vector3 worldPosition)
    {
        var root = new GameObject("EnemyBase");
        root.transform.position = worldPosition;

        var pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pillar.name = "BasePillar";
        pillar.transform.SetParent(root.transform, false);
        pillar.transform.localPosition = Vector3.up * 0.55f;
        pillar.transform.localScale = new Vector3(1.8f, 1.1f, 1.8f);

        var collider = pillar.GetComponent<Collider>();
        if (collider != null)
            Object.Destroy(collider);

        var renderer = pillar.GetComponent<Renderer>();
        if (renderer != null)
            renderer.material = CwslMaterialUtil.CreateMatteColored(BaseColor);

        var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "BaseRing";
        ring.transform.SetParent(root.transform, false);
        ring.transform.localPosition = new Vector3(0f, 0.06f, 0f);
        ring.transform.localScale = new Vector3(2.6f, 0.03f, 2.6f);
        var ringCollider = ring.GetComponent<Collider>();
        if (ringCollider != null)
            Object.Destroy(ringCollider);
        var ringRenderer = ring.GetComponent<Renderer>();
        if (ringRenderer != null)
            ringRenderer.material = CwslMaterialUtil.CreateMatteColored(new Color(1f, 0.55f, 0.12f, 0.55f));

        var marker = root.AddComponent<CwslEnemyBaseMarker>();
        marker.SpawnPosition = worldPosition;
        return marker;
    }
}
