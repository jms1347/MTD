using UnityEngine;

/// <summary>언월도 무기 (교체 가능).</summary>
public static class StllWeaponVisualBuilder
{
    public struct Result
    {
        public Transform WeaponRoot;
        public Transform BladePivot;
    }

    public static Result BuildGlaive(Transform handSocket, Color accentColor)
    {
        var weaponRoot = new GameObject("WeaponRoot").transform;
        weaponRoot.SetParent(handSocket, false);
        weaponRoot.localPosition = Vector3.zero;
        weaponRoot.localRotation = Quaternion.Euler(0f, 0f, -18f);

        var poleColor = new Color(0.35f, 0.22f, 0.12f);
        var bladeColor = Color.Lerp(accentColor, new Color(0.75f, 0.78f, 0.85f), 0.45f);
        var edgeColor = Color.Lerp(bladeColor, Color.white, 0.35f);

        StllVisualUtil.CreatePrimitive(PrimitiveType.Cylinder, weaponRoot, new Vector3(0f, 0f, 0.55f),
            new Vector3(0.06f, 0.06f, 1.1f), poleColor);

        var bladePivot = new GameObject("BladePivot").transform;
        bladePivot.SetParent(weaponRoot, false);
        bladePivot.localPosition = new Vector3(0f, 0f, 1.15f);

        StllVisualUtil.CreatePrimitive(PrimitiveType.Cube, bladePivot, new Vector3(0f, 0f, 0.18f),
            new Vector3(0.08f, 0.32f, 0.52f), bladeColor);
        StllVisualUtil.CreatePrimitive(PrimitiveType.Cube, bladePivot, new Vector3(0f, 0.18f, 0.34f),
            new Vector3(0.06f, 0.12f, 0.28f), edgeColor);
        StllVisualUtil.CreatePrimitive(PrimitiveType.Cube, bladePivot, new Vector3(0f, -0.16f, 0.1f),
            new Vector3(0.05f, 0.1f, 0.18f), edgeColor);

        return new Result
        {
            WeaponRoot = weaponRoot,
            BladePivot = bladePivot
        };
    }
}
