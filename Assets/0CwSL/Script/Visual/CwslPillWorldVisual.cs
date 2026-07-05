using UnityEngine;

public class CwslPillWorldVisual : MonoBehaviour
{
    private static readonly Color BlueColor = new(0.28f, 0.58f, 1f);
    private static readonly Color GreenColor = new(0.32f, 0.95f, 0.42f);
    private static readonly Color YellowColor = new(1f, 0.9f, 0.22f);

    private Renderer pillRenderer;
    private CwslPillPickup pickup;

    private void Awake()
    {
        pillRenderer = GetComponent<Renderer>();
        pickup = GetComponentInParent<CwslPillPickup>();
    }

    private void Update()
    {
        if (pickup == null || pillRenderer == null)
            return;

        ApplyColor(ResolveColor(pickup.PillType));
    }

    public static Color ResolveColor(CwslPillType type)
    {
        return type switch
        {
            CwslPillType.Blue => BlueColor,
            CwslPillType.Green => GreenColor,
            CwslPillType.Yellow => YellowColor,
            _ => BlueColor
        };
    }

    private void ApplyColor(Color color)
    {
        if (pillRenderer.sharedMaterial == null)
            pillRenderer.sharedMaterial = CwslMaterialUtil.CreateColored(color);
        else
            CwslMaterialUtil.ApplyColor(pillRenderer, color);
    }
}
