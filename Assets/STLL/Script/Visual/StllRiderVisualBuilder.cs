using UnityEngine;

/// <summary>레고 느낌 장수 (교체 가능).</summary>
public static class StllRiderVisualBuilder
{
    public struct Result
    {
        public Transform RiderRoot;
        public Transform RightHandSocket;
        public Transform HeadPivot;
    }

    public static Result Build(Transform mountPoint, Color accentColor)
    {
        var riderRoot = new GameObject("RiderRoot").transform;
        riderRoot.SetParent(mountPoint, false);
        riderRoot.localPosition = Vector3.zero;
        riderRoot.localRotation = Quaternion.identity;

        var armorColor = Color.Lerp(accentColor, new Color(0.85f, 0.2f, 0.15f), 0.35f);
        var trimColor = Color.Lerp(armorColor, Color.black, 0.25f);
        var skinColor = new Color(0.92f, 0.72f, 0.58f);

        StllVisualUtil.CreatePrimitive(PrimitiveType.Cube, riderRoot, new Vector3(0f, 0.08f, 0f),
            new Vector3(0.52f, 0.42f, 0.36f), armorColor);
        StllVisualUtil.CreatePrimitive(PrimitiveType.Cube, riderRoot, new Vector3(0f, 0.08f, -0.18f),
            new Vector3(0.46f, 0.38f, 0.08f), trimColor);

        var headPivot = new GameObject("HeadPivot").transform;
        headPivot.SetParent(riderRoot, false);
        headPivot.localPosition = new Vector3(0f, 0.42f, 0.02f);

        StllVisualUtil.CreatePrimitive(PrimitiveType.Cube, headPivot, new Vector3(0f, 0.12f, 0.02f),
            new Vector3(0.34f, 0.34f, 0.3f), skinColor);
        StllVisualUtil.CreatePrimitive(PrimitiveType.Cube, headPivot, new Vector3(0f, 0.24f, -0.04f),
            new Vector3(0.36f, 0.12f, 0.28f), trimColor);
        StllVisualUtil.CreatePrimitive(PrimitiveType.Cube, headPivot, new Vector3(-0.08f, 0.12f, 0.14f),
            new Vector3(0.05f, 0.04f, 0.03f), Color.black);
        StllVisualUtil.CreatePrimitive(PrimitiveType.Cube, headPivot, new Vector3(0.08f, 0.12f, 0.14f),
            new Vector3(0.05f, 0.04f, 0.03f), Color.black);

        AddLegoLimb(riderRoot, "ArmL", new Vector3(-0.34f, 0.1f, 0.06f), armorColor);
        var armR = AddLegoLimb(riderRoot, "ArmR", new Vector3(0.34f, 0.1f, 0.06f), armorColor);

        var handSocket = new GameObject("RightHandSocket").transform;
        handSocket.SetParent(armR, false);
        handSocket.localPosition = new Vector3(0f, -0.22f, 0.12f);

        return new Result
        {
            RiderRoot = riderRoot,
            RightHandSocket = handSocket,
            HeadPivot = headPivot
        };
    }

    private static Transform AddLegoLimb(Transform parent, string name, Vector3 localPos, Color color)
    {
        var limb = new GameObject(name).transform;
        limb.SetParent(parent, false);
        limb.localPosition = localPos;
        StllVisualUtil.CreatePrimitive(PrimitiveType.Cube, limb, Vector3.zero,
            new Vector3(0.14f, 0.36f, 0.14f), color);
        StllVisualUtil.CreatePrimitive(PrimitiveType.Cube, limb, new Vector3(0f, -0.24f, 0f),
            new Vector3(0.12f, 0.22f, 0.12f), color);
        return limb;
    }
}
