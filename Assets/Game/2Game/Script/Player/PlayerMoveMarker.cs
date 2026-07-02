using UnityEngine;

/// <summary>
/// 플레이어 이동 클릭 지점에 잠깐 표시되는 링 마커.
/// </summary>
public class PlayerMoveMarker : MonoBehaviour
{
    private static PlayerMoveMarker instance;

    [SerializeField] private float lifetime = 1.1f;
    [SerializeField] private float startScale = 0.55f;
    [SerializeField] private float endScale = 1.15f;

    private Renderer cachedRenderer;
    private Color baseColor = new(0.35f, 0.9f, 1f, 0.85f);
    private float hideTime;

    public static void Show(Vector3 worldPoint)
    {
        EnsureInstance();
        instance.Display(worldPoint);
    }

    private static void EnsureInstance()
    {
        if (instance != null)
            return;

        var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "PlayerMoveMarker";
        Object.Destroy(ring.GetComponent<Collider>());

        ring.transform.localScale = new Vector3(0.8f, 0.02f, 0.8f);
        var renderer = ring.GetComponent<Renderer>();
        if (renderer != null)
        {
            var material = new Material(Shader.Find("Standard"));
            material.color = new Color(0.35f, 0.9f, 1f, 0.85f);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", new Color(0.2f, 0.7f, 1f) * 0.6f);
            renderer.material = material;
        }

        instance = ring.AddComponent<PlayerMoveMarker>();
        instance.cachedRenderer = renderer;
        ring.SetActive(false);
    }

    private void Display(Vector3 worldPoint)
    {
        transform.position = new Vector3(worldPoint.x, 0.07f, worldPoint.z);
        transform.localScale = new Vector3(startScale, 0.02f, startScale);
        hideTime = Time.time + lifetime;
        gameObject.SetActive(true);
    }

    private void Update()
    {
        if (!gameObject.activeSelf)
            return;

        float remaining = hideTime - Time.time;
        if (remaining <= 0f)
        {
            gameObject.SetActive(false);
            return;
        }

        float t = 1f - remaining / lifetime;
        float scale = Mathf.Lerp(startScale, endScale, t);
        transform.localScale = new Vector3(scale, 0.02f, scale);

        if (cachedRenderer != null && cachedRenderer.material != null)
        {
            var color = baseColor;
            color.a = Mathf.Lerp(baseColor.a, 0f, t);
            cachedRenderer.material.color = color;
        }
    }
}
