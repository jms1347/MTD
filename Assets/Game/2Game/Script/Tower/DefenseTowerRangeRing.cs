using UnityEngine;

/// <summary>
/// 선택된 타워 사정거리 끝에 원형 테두리를 표시합니다.
/// </summary>
public class DefenseTowerRangeRing : MonoBehaviour
{
    private const int Segments = 80;
    private const float GroundY = 0.06f;
    private const float LineWidth = 0.14f;

    private static DefenseTowerRangeRing instance;

    private LineRenderer line;
    private Transform followTarget;
    private float radius;
    private Vector3[] segmentBuffer;

    public static void Show(Transform tower, float range)
    {
        if (tower == null || range <= 0f)
        {
            Hide();
            return;
        }

        EnsureInstance();
        instance.followTarget = tower;
        instance.radius = range;
        instance.gameObject.SetActive(true);
        instance.RefreshCircle();
    }

    public static void Hide()
    {
        if (instance == null)
            return;

        instance.followTarget = null;
        instance.gameObject.SetActive(false);
    }

    private static void EnsureInstance()
    {
        if (instance != null)
            return;

        var root = new GameObject("TowerRangeRing");
        instance = root.AddComponent<DefenseTowerRangeRing>();
        instance.BuildLineRenderer();
        root.SetActive(false);
    }

    private void BuildLineRenderer()
    {
        line = gameObject.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.loop = true;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        line.widthMultiplier = LineWidth;
        line.numCornerVertices = 4;
        line.numCapVertices = 4;
        line.positionCount = Segments;
        line.alignment = LineAlignment.View;
        line.sortingOrder = 50;

        var color = new Color(0.35f, 0.9f, 1f, 0.92f);
        line.startColor = color;
        line.endColor = color;

        var material = new Material(Shader.Find("Sprites/Default"));
        material.color = color;
        line.material = material;
        line.textureMode = LineTextureMode.Stretch;

        segmentBuffer = new Vector3[Segments];
    }

    private void LateUpdate()
    {
        if (!gameObject.activeSelf)
            return;

        if (followTarget == null || !followTarget.gameObject.activeInHierarchy)
        {
            Hide();
            return;
        }

        RefreshCircle();
    }

    private void RefreshCircle()
    {
        if (line == null || followTarget == null)
            return;

        Vector3 center = followTarget.position;
        center.y = GroundY;

        for (int i = 0; i < Segments; i++)
        {
            float angle = i * Mathf.PI * 2f / Segments;
            segmentBuffer[i] = new Vector3(
                center.x + Mathf.Cos(angle) * radius,
                GroundY,
                center.z + Mathf.Sin(angle) * radius);
        }

        line.SetPositions(segmentBuffer);
    }
}
