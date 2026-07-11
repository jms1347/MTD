using UnityEngine;

/// <summary>질주자 말과 동일한 프로시저럴 말 (교체 가능).</summary>
public static class StllHorseVisualBuilder
{
    public struct Result
    {
        public Transform HorseRoot;
        public Transform LegFl;
        public Transform LegFr;
        public Transform LegBl;
        public Transform LegBr;
        public Transform RiderMountPoint;
    }

    public static Result Build(Transform parent, Color accentColor)
    {
        var horseRoot = new GameObject("HorseRoot").transform;
        horseRoot.SetParent(parent, false);

        var horseColor = Color.Lerp(accentColor, new Color(0.38f, 0.24f, 0.12f), 0.48f);
        var maneColor = Color.Lerp(accentColor, Color.black, 0.22f);
        var hoofColor = new Color(0.18f, 0.14f, 0.12f);

        StllVisualUtil.CreatePrimitive(PrimitiveType.Cylinder, horseRoot, new Vector3(0f, 0.03f, 0f),
            new Vector3(1.28f, 0.03f, 1.55f), new Color(0.08f, 0.08f, 0.1f, 0.65f));

        var horseBody = StllVisualUtil.CreatePrimitive(PrimitiveType.Capsule, horseRoot, new Vector3(0f, 0.78f, 0f),
            new Vector3(0.95f, 0.52f, 0.42f), horseColor);
        horseBody.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        StllVisualUtil.CreatePrimitive(PrimitiveType.Cube, horseRoot, new Vector3(0f, 1.02f, 0.48f),
            new Vector3(0.34f, 0.32f, 0.42f), horseColor);
        var neck = StllVisualUtil.CreatePrimitive(PrimitiveType.Cylinder, horseRoot, new Vector3(0f, 0.98f, 0.24f),
            new Vector3(0.22f, 0.18f, 0.22f), horseColor);
        neck.transform.localRotation = Quaternion.Euler(58f, 0f, 0f);
        StllVisualUtil.CreatePrimitive(PrimitiveType.Cube, horseRoot, new Vector3(0f, 1.08f, 0.08f),
            new Vector3(0.08f, 0.28f, 0.42f), maneColor);
        StllVisualUtil.CreatePrimitive(PrimitiveType.Cube, horseRoot, new Vector3(0f, 0.82f, -0.52f),
            new Vector3(0.08f, 0.34f, 0.24f), maneColor);

        var legFl = AddLeg(horseRoot, "HorseLegFL", new Vector3(-0.26f, 0.42f, 0.34f), horseColor, hoofColor);
        var legFr = AddLeg(horseRoot, "HorseLegFR", new Vector3(0.26f, 0.42f, 0.34f), horseColor, hoofColor);
        var legBl = AddLeg(horseRoot, "HorseLegBL", new Vector3(-0.26f, 0.42f, -0.34f), horseColor, hoofColor);
        var legBr = AddLeg(horseRoot, "HorseLegBR", new Vector3(0.26f, 0.42f, -0.34f), horseColor, hoofColor);

        var mountPoint = new GameObject("RiderMountPoint").transform;
        mountPoint.SetParent(horseRoot, false);
        mountPoint.localPosition = new Vector3(0f, 1.08f, -0.04f);

        return new Result
        {
            HorseRoot = horseRoot,
            LegFl = legFl,
            LegFr = legFr,
            LegBl = legBl,
            LegBr = legBr,
            RiderMountPoint = mountPoint
        };
    }

    private static Transform AddLeg(Transform parent, string name, Vector3 localPos, Color legColor, Color hoofColor)
    {
        var legRoot = new GameObject(name).transform;
        legRoot.SetParent(parent, false);
        legRoot.localPosition = localPos;
        StllVisualUtil.CreatePrimitive(PrimitiveType.Cylinder, legRoot, new Vector3(0f, -0.12f, 0f),
            new Vector3(0.14f, 0.28f, 0.14f), legColor);
        StllVisualUtil.CreatePrimitive(PrimitiveType.Cube, legRoot, new Vector3(0f, -0.34f, 0f),
            new Vector3(0.16f, 0.08f, 0.18f), hoofColor);
        return legRoot;
    }
}
