using UnityEngine;

public static class CwslInkOctopusVisualBuilder
{
    public static void Build(Transform root, CwslMonsterPalette palette)
    {
        var bodyColor = palette.Primary;
        var underside = palette.Secondary;
        var eyeWhite = Color.white;
        var pupil = palette.Accent;

        var body = CwslMonsterVisualBuilder.CreatePrimitivePublic(
            PrimitiveType.Sphere, root, new Vector3(0f, 0.62f, 0f), new Vector3(0.72f, 0.58f, 0.68f), bodyColor);
        body.name = "Body";

        var mantle = CwslMonsterVisualBuilder.CreatePrimitivePublic(
            PrimitiveType.Sphere, root, new Vector3(0f, 0.82f, -0.04f), new Vector3(0.56f, 0.34f, 0.52f), underside);
        mantle.name = "Mantle";

        var eyeL = CwslMonsterVisualBuilder.CreatePrimitivePublic(
            PrimitiveType.Sphere, root, new Vector3(-0.16f, 0.72f, 0.24f), new Vector3(0.12f, 0.12f, 0.08f), eyeWhite);
        var eyeR = CwslMonsterVisualBuilder.CreatePrimitivePublic(
            PrimitiveType.Sphere, root, new Vector3(0.16f, 0.72f, 0.24f), new Vector3(0.12f, 0.12f, 0.08f), eyeWhite);
        var pupilL = CwslMonsterVisualBuilder.CreatePrimitivePublic(
            PrimitiveType.Sphere, eyeL.transform, new Vector3(0f, 0f, 0.05f), new Vector3(0.45f, 0.45f, 0.35f), pupil);
        var pupilR = CwslMonsterVisualBuilder.CreatePrimitivePublic(
            PrimitiveType.Sphere, eyeR.transform, new Vector3(0f, 0f, 0.05f), new Vector3(0.45f, 0.45f, 0.35f), pupil);

        for (var i = 0; i < 8; i++)
        {
            var angle = i * 45f + 22.5f;
            var rad = angle * Mathf.Deg2Rad;
            var dir = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
            var tentacleRoot = new GameObject("Tentacle_" + i);
            tentacleRoot.transform.SetParent(root, false);
            tentacleRoot.transform.localPosition = new Vector3(dir.x * 0.28f, 0.42f, dir.z * 0.28f);
            tentacleRoot.transform.localRotation = Quaternion.LookRotation(dir + Vector3.down * 0.35f, Vector3.up);

            var tentacle = CwslMonsterVisualBuilder.CreatePrimitivePublic(
                PrimitiveType.Cylinder,
                tentacleRoot.transform,
                new Vector3(0f, -0.18f, 0.12f),
                new Vector3(0.07f, 0.22f, 0.07f),
                Color.Lerp(bodyColor, underside, 0.35f));
            tentacle.transform.localRotation = Quaternion.Euler(18f, 0f, 0f);
        }

        var cannonPivot = new GameObject("CannonPivot");
        cannonPivot.transform.SetParent(root, false);
        cannonPivot.transform.localPosition = new Vector3(0f, 0.68f, 0.18f);
        cannonPivot.transform.localRotation = Quaternion.Euler(10f, 0f, 0f);

        var inkNozzle = CwslMonsterVisualBuilder.CreatePrimitivePublic(
            PrimitiveType.Cylinder, cannonPivot.transform, new Vector3(0f, 0f, 0.16f),
            new Vector3(0.08f, 0.12f, 0.08f), palette.Metal);
        inkNozzle.name = "Cannon";
        inkNozzle.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        var muzzle = new GameObject("Muzzle");
        muzzle.transform.SetParent(cannonPivot.transform, false);
        muzzle.transform.localPosition = new Vector3(0f, 0f, 0.28f);

        CwslMonsterVisualBuilder.RemoveCollidersPublic(body, mantle, eyeL, eyeR, pupilL, pupilR, inkNozzle);
        foreach (var collider in root.GetComponentsInChildren<Collider>(true))
            collider.enabled = false;

        root.gameObject.AddComponent<CwslInkOctopusTentacleVisual>();
    }
}
