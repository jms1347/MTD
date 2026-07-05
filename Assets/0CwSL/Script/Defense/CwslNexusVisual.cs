using UnityEngine;

/// <summary>넥서스 비주얼 — 성/탑 실루엣 + 금색 발광 코어.</summary>
public class CwslNexusVisual : MonoBehaviour
{
    private static readonly Color BodyColor = new(1f, 0.84f, 0.2f, 1f);
    private static readonly Color EmissionColor = new(1f, 0.72f, 0.12f, 1f);
    private static readonly Color StoneColor = new(0.42f, 0.44f, 0.5f, 1f);

    private static Material sharedGoldMaterial;
    private static Material sharedEmissiveMaterial;
    private static Material sharedStoneMaterial;

    private void Awake()
    {
        Rebuild();
    }

    public void Rebuild()
    {
        CwslNexusVisualBuilder.Build(transform);
        EnsureCollider();
    }

    private void EnsureCollider()
    {
        var collider = GetComponent<CapsuleCollider>();
        if (collider != null)
            CwslNexusVisualBuilder.ConfigureHitCollider(collider);
    }

    public static Material GetOrCreateMaterial()
    {
        return GetGoldMaterial(BodyColor);
    }

    public static Material GetStoneMaterial(Color color)
    {
        if (sharedStoneMaterial == null || !CwslMaterialUtil.IsMaterialValid(sharedStoneMaterial))
        {
            sharedStoneMaterial = CwslMaterialUtil.CreateMatteColored(StoneColor);
            sharedStoneMaterial.name = "CwslNexusStoneMaterial";
        }

        var instance = new Material(sharedStoneMaterial)
        {
            name = "CwslNexusStoneInstance"
        };
        if (instance.HasProperty("_BaseColor"))
            instance.SetColor("_BaseColor", color);
        instance.color = color;
        return instance;
    }

    public static Material GetGoldMaterial(Color color)
    {
        if (sharedGoldMaterial != null && CwslMaterialUtil.IsMaterialValid(sharedGoldMaterial))
        {
            var tinted = new Material(sharedGoldMaterial) { name = "CwslNexusGoldInstance" };
            if (tinted.HasProperty("_BaseColor"))
                tinted.SetColor("_BaseColor", color);
            tinted.color = color;
            return tinted;
        }

        sharedGoldMaterial = CwslMaterialUtil.CreateColored(color);
        sharedGoldMaterial.name = "CwslNexusGoldMaterial";
        CwslMaterialUtil.ApplyMatteProperties(sharedGoldMaterial);
        return sharedGoldMaterial;
    }

    public static Material GetEmissiveMaterial(Color color)
    {
        if (sharedEmissiveMaterial == null || !CwslMaterialUtil.IsMaterialValid(sharedEmissiveMaterial))
        {
            sharedEmissiveMaterial = CwslMaterialUtil.CreateColored(BodyColor);
            sharedEmissiveMaterial.name = "CwslNexusEmissiveMaterial";

            if (sharedEmissiveMaterial.HasProperty("_EmissionColor"))
            {
                sharedEmissiveMaterial.EnableKeyword("_EMISSION");
                sharedEmissiveMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                sharedEmissiveMaterial.SetColor("_EmissionColor", EmissionColor * 0.85f);
            }

            CwslMaterialUtil.ApplyMatteProperties(sharedEmissiveMaterial);
        }

        var instance = new Material(sharedEmissiveMaterial)
        {
            name = "CwslNexusEmissiveInstance"
        };
        if (instance.HasProperty("_BaseColor"))
            instance.SetColor("_BaseColor", color);
        instance.color = color;
        if (instance.HasProperty("_EmissionColor"))
            instance.SetColor("_EmissionColor", color * 0.75f);
        return instance;
    }

    public static void ApplyTo(Transform root)
    {
        if (root == null)
            return;

        var visual = root.GetComponent<CwslNexusVisual>();
        if (visual != null)
        {
            visual.Rebuild();
            return;
        }

        CwslNexusVisualBuilder.Build(root);
        var collider = root.GetComponent<CapsuleCollider>();
        if (collider != null)
            CwslNexusVisualBuilder.ConfigureHitCollider(collider);
    }
}
