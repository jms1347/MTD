using UnityEngine;

public static class CoopTankVisualFactory
{
    public struct TankVisualRefs
    {
        public Transform Hull;
        public Transform Turret;
        public Transform FirePoint;
    }

    public static TankVisualRefs Build(Transform root, CoopTankDefinition tank)
    {
        var hull = CreatePart(root, "Hull", PrimitiveType.Cube, new Vector3(0f, tank.HullHeight * 0.5f + 0.12f, 0f),
            new Vector3(tank.HullWidth, tank.HullHeight, tank.HullLength), tank.HullColor);

        CreatePart(root, "TrackLeft", PrimitiveType.Cube, new Vector3(-tank.HullWidth * 0.62f, 0.1f, 0f),
            new Vector3(0.22f, 0.18f, tank.HullLength * 1.05f), tank.TrackColor);
        CreatePart(root, "TrackRight", PrimitiveType.Cube, new Vector3(tank.HullWidth * 0.62f, 0.1f, 0f),
            new Vector3(0.22f, 0.18f, tank.HullLength * 1.05f), tank.TrackColor);

        var turretBase = CreatePart(root, "TurretBase", PrimitiveType.Cylinder,
            new Vector3(0f, tank.HullHeight + 0.22f, -tank.HullLength * 0.05f),
            new Vector3(0.55f, 0.12f, 0.55f), tank.TurretColor);

        var turret = new GameObject("Turret").transform;
        turret.SetParent(root, false);
        turret.localPosition = turretBase.localPosition;

        var barrel = CreatePart(turret, "Barrel", PrimitiveType.Cube,
            new Vector3(0f, 0.05f, tank.HullLength * 0.42f),
            new Vector3(0.16f, 0.16f, tank.HullLength * 0.72f), tank.TurretColor * 0.9f);

        var firePoint = new GameObject("FirePoint").transform;
        firePoint.SetParent(turret, false);
        firePoint.localPosition = barrel.localPosition + new Vector3(0f, 0f, tank.HullLength * 0.36f);

        EnsurePickCollider(root, tank);
        return new TankVisualRefs
        {
            Hull = hull,
            Turret = turret,
            FirePoint = firePoint
        };
    }

    private static Transform CreatePart(Transform parent, string name, PrimitiveType type, Vector3 localPos, Vector3 scale, Color color)
    {
        var part = GameObject.CreatePrimitive(type);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPos;
        part.transform.localScale = scale;
        UnityEngine.Object.Destroy(part.GetComponent<Collider>());

        var renderer = part.GetComponent<Renderer>();
        if (renderer != null)
            renderer.material.color = color;

        return part.transform;
    }

    private static void EnsurePickCollider(Transform root, CoopTankDefinition tank)
    {
        var collider = root.gameObject.GetComponent<CapsuleCollider>();
        if (collider == null)
            collider = root.gameObject.AddComponent<CapsuleCollider>();

        collider.isTrigger = false;
        collider.direction = 1;
        collider.radius = tank.HullWidth * 0.45f;
        collider.height = Mathf.Max(0.8f, tank.HullLength);
        collider.center = new Vector3(0f, 0.45f, 0f);
    }
}
