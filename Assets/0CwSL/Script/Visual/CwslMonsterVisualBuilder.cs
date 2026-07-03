using UnityEngine;

public static class CwslMonsterVisualBuilder
{
    public static void Build(Transform root, CwslMonsterType type)
    {
        var visualRoot = new GameObject("Visual");
        visualRoot.transform.SetParent(root, false);

        switch (type)
        {
            case CwslMonsterType.Ranged:
                BuildRanged(visualRoot.transform);
                break;
            case CwslMonsterType.Suicide:
                BuildSuicide(visualRoot.transform);
                break;
            case CwslMonsterType.Melee:
                BuildMelee(visualRoot.transform);
                break;
            case CwslMonsterType.BossHongmyeongbo:
                BuildBoss(visualRoot.transform);
                break;
        }
    }

    private static void BuildRanged(Transform root)
    {
        var baseCube = CreatePrimitive(PrimitiveType.Cube, root, new Vector3(0f, 0.35f, 0f), new Vector3(1.1f, 0.35f, 1.1f),
            new Color(0.28f, 0.22f, 0.45f));
        var body = CreatePrimitive(PrimitiveType.Sphere, root, new Vector3(0f, 0.95f, 0f), Vector3.one * 0.95f,
            new Color(0.55f, 0.25f, 0.95f));
        var eyeL = CreatePrimitive(PrimitiveType.Sphere, body.transform, new Vector3(-0.22f, 0.12f, 0.38f), Vector3.one * 0.18f,
            new Color(0.95f, 0.2f, 0.85f));
        var eyeR = CreatePrimitive(PrimitiveType.Sphere, body.transform, new Vector3(0.22f, 0.12f, 0.38f), Vector3.one * 0.18f,
            new Color(0.95f, 0.2f, 0.85f));
        var cannonPivot = new GameObject("CannonPivot");
        cannonPivot.transform.SetParent(root, false);
        cannonPivot.transform.localPosition = new Vector3(0f, 1.05f, 0.22f);
        cannonPivot.transform.localRotation = Quaternion.Euler(12f, 0f, 0f);

        var cannon = CreatePrimitive(PrimitiveType.Cylinder, cannonPivot.transform, new Vector3(0f, 0f, 0.22f),
            new Vector3(0.22f, 0.35f, 0.22f), new Color(0.15f, 0.12f, 0.2f));
        cannon.name = "Cannon";
        cannon.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        var muzzle = new GameObject("Muzzle");
        muzzle.transform.SetParent(cannonPivot.transform, false);
        muzzle.transform.localPosition = new Vector3(0f, 0f, 0.48f);

        RemoveCollider(baseCube);
        RemoveCollider(body);
        RemoveCollider(eyeL);
        RemoveCollider(eyeR);
        RemoveCollider(cannon);
    }

    private static void BuildSuicide(Transform root)
    {
        var core = CreatePrimitive(PrimitiveType.Sphere, root, new Vector3(0f, 0.65f, 0f), Vector3.one * 0.85f,
            new Color(1f, 0.45f, 0.1f));
        for (var i = 0; i < 6; i++)
        {
            var angle = i * 60f * Mathf.Deg2Rad;
            var spike = CreatePrimitive(PrimitiveType.Cube, root,
                new Vector3(Mathf.Cos(angle) * 0.55f, 0.65f, Mathf.Sin(angle) * 0.55f),
                new Vector3(0.18f, 0.55f, 0.18f), new Color(1f, 0.2f, 0.05f));
            spike.transform.localRotation = Quaternion.Euler(0f, i * 60f, 0f);
            RemoveCollider(spike);
        }

        var fuse = CreatePrimitive(PrimitiveType.Cylinder, root, new Vector3(0f, 1.15f, 0f), new Vector3(0.08f, 0.2f, 0.08f),
            new Color(0.2f, 0.2f, 0.2f));
        RemoveCollider(core);
        RemoveCollider(fuse);
    }

    private static void BuildMelee(Transform root)
    {
        var body = CreatePrimitive(PrimitiveType.Sphere, root, new Vector3(0f, 0.7f, 0f), new Vector3(1.15f, 0.95f, 1.15f),
            new Color(0.2f, 0.85f, 0.35f));
        var jawL = CreatePrimitive(PrimitiveType.Cube, root, new Vector3(-0.35f, 0.45f, 0.35f), new Vector3(0.25f, 0.18f, 0.35f),
            new Color(0.1f, 0.55f, 0.2f));
        var jawR = CreatePrimitive(PrimitiveType.Cube, root, new Vector3(0.35f, 0.45f, 0.35f), new Vector3(0.25f, 0.18f, 0.35f),
            new Color(0.1f, 0.55f, 0.2f));
        RemoveCollider(body);
        RemoveCollider(jawL);
        RemoveCollider(jawR);
    }

    private static void BuildBoss(Transform root)
    {
        var lower = CreatePrimitive(PrimitiveType.Cylinder, root, new Vector3(0f, 0.9f, 0f), new Vector3(2.4f, 0.9f, 2.4f),
            new Color(0.15f, 0.35f, 0.95f));
        var upper = CreatePrimitive(PrimitiveType.Sphere, root, new Vector3(0f, 2.2f, 0f), Vector3.one * 2.1f,
            new Color(0.9f, 0.12f, 0.1f));
        var crown = CreatePrimitive(PrimitiveType.Cube, root, new Vector3(0f, 3.35f, 0f), new Vector3(1.2f, 0.35f, 1.2f),
            new Color(0.95f, 0.85f, 0.2f));
        RemoveCollider(lower);
        RemoveCollider(upper);
        RemoveCollider(crown);
    }

    public static void BuildPlayer(Transform root, Color bodyColor)
    {
        var visualRoot = new GameObject("Visual");
        visualRoot.transform.SetParent(root, false);

        var armorColor = bodyColor;
        var trimColor = Color.Lerp(bodyColor, Color.black, 0.38f);
        var metalColor = new Color(0.72f, 0.76f, 0.82f);
        var emblemColor = Color.Lerp(bodyColor, Color.white, 0.45f);
        var capeColor = Color.Lerp(bodyColor, new Color(0.2f, 0.25f, 0.35f), 0.35f);

        var basePlate = CreatePrimitive(PrimitiveType.Cylinder, visualRoot.transform, new Vector3(0f, 0.08f, 0f),
            new Vector3(1.05f, 0.06f, 1.05f), trimColor);
        var body = CreatePrimitive(PrimitiveType.Capsule, visualRoot.transform, new Vector3(0f, 0.92f, -0.08f),
            new Vector3(0.82f, 0.88f, 0.82f), armorColor);
        var chestPlate = CreatePrimitive(PrimitiveType.Cube, visualRoot.transform, new Vector3(0f, 1.02f, 0.12f),
            new Vector3(0.62f, 0.52f, 0.28f), emblemColor);
        var shoulderL = CreatePrimitive(PrimitiveType.Sphere, visualRoot.transform, new Vector3(-0.48f, 1.18f, -0.02f),
            new Vector3(0.38f, 0.32f, 0.38f), trimColor);
        var shoulderR = CreatePrimitive(PrimitiveType.Sphere, visualRoot.transform, new Vector3(0.48f, 1.18f, -0.02f),
            new Vector3(0.38f, 0.32f, 0.38f), trimColor);
        var helm = CreatePrimitive(PrimitiveType.Sphere, visualRoot.transform, new Vector3(0f, 1.62f, -0.06f),
            new Vector3(0.48f, 0.42f, 0.48f), armorColor);
        var visor = CreatePrimitive(PrimitiveType.Cube, visualRoot.transform, new Vector3(0f, 1.58f, 0.14f),
            new Vector3(0.34f, 0.1f, 0.08f), metalColor);

        var shieldRoot = new GameObject("Shield");
        shieldRoot.transform.SetParent(visualRoot.transform, false);
        shieldRoot.transform.localPosition = new Vector3(0f, 1.02f, 0.7f);

        var shieldBack = CreatePrimitive(PrimitiveType.Cube, shieldRoot.transform, new Vector3(0f, 0f, -0.08f),
            new Vector3(1.18f, 1.42f, 0.12f), trimColor);
        var shieldFace = CreatePrimitive(PrimitiveType.Cube, shieldRoot.transform, new Vector3(0f, 0f, 0.02f),
            new Vector3(1.05f, 1.28f, 0.1f), armorColor);
        var shieldRimTop = CreatePrimitive(PrimitiveType.Cube, shieldRoot.transform, new Vector3(0f, 0.7f, 0f),
            new Vector3(1.12f, 0.1f, 0.14f), metalColor);
        var shieldRimBottom = CreatePrimitive(PrimitiveType.Cube, shieldRoot.transform, new Vector3(0f, -0.68f, 0f),
            new Vector3(1.12f, 0.1f, 0.14f), metalColor);
        var shieldRimL = CreatePrimitive(PrimitiveType.Cube, shieldRoot.transform, new Vector3(-0.58f, 0f, 0f),
            new Vector3(0.1f, 1.28f, 0.14f), metalColor);
        var shieldRimR = CreatePrimitive(PrimitiveType.Cube, shieldRoot.transform, new Vector3(0.58f, 0f, 0f),
            new Vector3(0.1f, 1.28f, 0.14f), metalColor);
        var shieldBoss = CreatePrimitive(PrimitiveType.Sphere, shieldRoot.transform, new Vector3(0f, 0.03f, 0.1f),
            new Vector3(0.28f, 0.28f, 0.16f), emblemColor);
        var shieldCrossV = CreatePrimitive(PrimitiveType.Cube, shieldRoot.transform, new Vector3(0f, 0.03f, 0.14f),
            new Vector3(0.12f, 0.72f, 0.06f), metalColor);
        var shieldCrossH = CreatePrimitive(PrimitiveType.Cube, shieldRoot.transform, new Vector3(0f, 0.03f, 0.14f),
            new Vector3(0.52f, 0.12f, 0.06f), metalColor);

        var backGuard = CreatePrimitive(PrimitiveType.Cube, visualRoot.transform, new Vector3(0f, 0.95f, -0.42f),
            new Vector3(0.72f, 0.95f, 0.18f), capeColor);
        var auraRing = CreatePrimitive(PrimitiveType.Cylinder, visualRoot.transform, new Vector3(0f, 0.04f, 0f),
            new Vector3(1.25f, 0.02f, 1.25f), Color.Lerp(armorColor, Color.white, 0.25f));

        RemoveCollider(basePlate);
        RemoveCollider(body);
        RemoveCollider(chestPlate);
        RemoveCollider(shoulderL);
        RemoveCollider(shoulderR);
        RemoveCollider(helm);
        RemoveCollider(visor);
        RemoveCollider(shieldBack);
        RemoveCollider(shieldFace);
        RemoveCollider(shieldRimTop);
        RemoveCollider(shieldRimBottom);
        RemoveCollider(shieldRimL);
        RemoveCollider(shieldRimR);
        RemoveCollider(shieldBoss);
        RemoveCollider(shieldCrossV);
        RemoveCollider(shieldCrossH);
        RemoveCollider(backGuard);
        RemoveCollider(auraRing);
    }

    public static void BuildMissileTankPlayer(Transform root, Color accentColor)
    {
        var visualRoot = new GameObject("Visual");
        visualRoot.transform.SetParent(root, false);
        visualRoot.AddComponent<CwslPlayerCannonRecoilVisual>();

        var hullColor = Color.Lerp(accentColor, new Color(0.18f, 0.2f, 0.24f), 0.42f);
        var trimColor = Color.Lerp(hullColor, Color.black, 0.28f);
        var metalColor = new Color(0.78f, 0.8f, 0.86f);
        var treadColor = new Color(0.12f, 0.12f, 0.14f);
        var glowColor = Color.Lerp(accentColor, new Color(1f, 0.55f, 0.15f), 0.35f);

        var shadowPlate = CreatePrimitive(PrimitiveType.Cylinder, visualRoot.transform, new Vector3(0f, 0.03f, 0f),
            new Vector3(1.35f, 0.03f, 1.55f), new Color(0.08f, 0.08f, 0.1f, 0.65f));
        var treadL = CreatePrimitive(PrimitiveType.Cube, visualRoot.transform, new Vector3(-0.72f, 0.22f, 0f),
            new Vector3(0.28f, 0.28f, 1.42f), treadColor);
        var treadR = CreatePrimitive(PrimitiveType.Cube, visualRoot.transform, new Vector3(0.72f, 0.22f, 0f),
            new Vector3(0.28f, 0.28f, 1.42f), treadColor);
        var treadDetailL = CreatePrimitive(PrimitiveType.Cube, visualRoot.transform, new Vector3(-0.72f, 0.22f, 0f),
            new Vector3(0.18f, 0.2f, 1.18f), trimColor);
        var treadDetailR = CreatePrimitive(PrimitiveType.Cube, visualRoot.transform, new Vector3(0.72f, 0.22f, 0f),
            new Vector3(0.18f, 0.2f, 1.18f), trimColor);
        var hull = CreatePrimitive(PrimitiveType.Cube, visualRoot.transform, new Vector3(0f, 0.52f, -0.02f),
            new Vector3(1.18f, 0.42f, 1.28f), hullColor);
        var hullTop = CreatePrimitive(PrimitiveType.Cube, visualRoot.transform, new Vector3(0f, 0.72f, -0.08f),
            new Vector3(0.92f, 0.12f, 0.92f), trimColor);
        var glacis = CreatePrimitive(PrimitiveType.Cube, visualRoot.transform, new Vector3(0f, 0.48f, 0.52f),
            new Vector3(0.95f, 0.18f, 0.28f), Color.Lerp(hullColor, accentColor, 0.25f));

        var turretBase = CreatePrimitive(PrimitiveType.Cylinder, visualRoot.transform, new Vector3(0f, 0.86f, -0.04f),
            new Vector3(0.78f, 0.14f, 0.78f), trimColor);
        var turret = CreatePrimitive(PrimitiveType.Cylinder, visualRoot.transform, new Vector3(0f, 1.02f, -0.02f),
            new Vector3(0.72f, 0.18f, 0.72f), hullColor);
        var turretCap = CreatePrimitive(PrimitiveType.Cylinder, visualRoot.transform, new Vector3(0f, 1.14f, -0.02f),
            new Vector3(0.58f, 0.08f, 0.58f), metalColor);
        var hatch = CreatePrimitive(PrimitiveType.Cylinder, visualRoot.transform, new Vector3(0.18f, 1.18f, -0.08f),
            new Vector3(0.18f, 0.05f, 0.18f), metalColor);
        var antenna = CreatePrimitive(PrimitiveType.Cylinder, visualRoot.transform, new Vector3(-0.22f, 1.24f, -0.12f),
            new Vector3(0.03f, 0.18f, 0.03f), metalColor);
        var antennaTip = CreatePrimitive(PrimitiveType.Sphere, visualRoot.transform, new Vector3(-0.22f, 1.34f, -0.12f),
            new Vector3(0.07f, 0.07f, 0.07f), glowColor);

        var cannonPivot = new GameObject("CannonPivot");
        cannonPivot.transform.SetParent(visualRoot.transform, false);
        cannonPivot.transform.localPosition = new Vector3(0f, 1.02f, 0.18f);
        cannonPivot.transform.localRotation = Quaternion.Euler(8f, 0f, 0f);

        var barrelBase = CreatePrimitive(PrimitiveType.Cylinder, cannonPivot.transform, new Vector3(0f, 0f, 0.12f),
            new Vector3(0.24f, 0.24f, 0.18f), trimColor);
        barrelBase.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var barrel = CreatePrimitive(PrimitiveType.Cylinder, cannonPivot.transform, new Vector3(0f, 0f, 0.48f),
            new Vector3(0.16f, 0.16f, 0.62f), metalColor);
        barrel.name = "Barrel";
        barrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var muzzleBrake = CreatePrimitive(PrimitiveType.Cylinder, cannonPivot.transform, new Vector3(0f, 0f, 0.82f),
            new Vector3(0.2f, 0.2f, 0.1f), glowColor);
        muzzleBrake.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        var muzzle = new GameObject("Muzzle");
        muzzle.transform.SetParent(cannonPivot.transform, false);
        muzzle.transform.localPosition = new Vector3(0f, 0f, 0.92f);

        var exhaustL = CreatePrimitive(PrimitiveType.Cube, visualRoot.transform, new Vector3(-0.28f, 0.62f, -0.58f),
            new Vector3(0.12f, 0.08f, 0.08f), treadColor);
        var exhaustR = CreatePrimitive(PrimitiveType.Cube, visualRoot.transform, new Vector3(0.28f, 0.62f, -0.58f),
            new Vector3(0.12f, 0.08f, 0.08f), treadColor);
        var stripe = CreatePrimitive(PrimitiveType.Cube, visualRoot.transform, new Vector3(0f, 0.58f, 0.58f),
            new Vector3(0.72f, 0.06f, 0.04f), accentColor);
        var glowRing = CreatePrimitive(PrimitiveType.Cylinder, visualRoot.transform, new Vector3(0f, 0.04f, 0f),
            new Vector3(1.42f, 0.02f, 1.62f), Color.Lerp(accentColor, Color.white, 0.2f));

        RemoveCollider(shadowPlate);
        RemoveCollider(treadL);
        RemoveCollider(treadR);
        RemoveCollider(treadDetailL);
        RemoveCollider(treadDetailR);
        RemoveCollider(hull);
        RemoveCollider(hullTop);
        RemoveCollider(glacis);
        RemoveCollider(turretBase);
        RemoveCollider(turret);
        RemoveCollider(turretCap);
        RemoveCollider(hatch);
        RemoveCollider(antenna);
        RemoveCollider(antennaTip);
        RemoveCollider(barrelBase);
        RemoveCollider(barrel);
        RemoveCollider(muzzleBrake);
        RemoveCollider(exhaustL);
        RemoveCollider(exhaustR);
        RemoveCollider(stripe);
        RemoveCollider(glowRing);
    }

    public static void BuildRedMagePlayer(Transform root, Color accentColor)
    {
        var visualRoot = new GameObject("Visual");
        visualRoot.transform.SetParent(root, false);

        var robeColor = Color.Lerp(new Color(0.75f, 0.08f, 0.08f), accentColor, 0.25f);
        var trimColor = new Color(0.95f, 0.75f, 0.2f);
        var hoodColor = Color.Lerp(robeColor, Color.black, 0.25f);
        var staffColor = new Color(0.35f, 0.22f, 0.12f);
        var gemColor = new Color(1f, 0.35f, 0.1f);

        var basePlate = CreatePrimitive(PrimitiveType.Cylinder, visualRoot.transform, new Vector3(0f, 0.05f, 0f),
            new Vector3(0.95f, 0.04f, 0.95f), Color.Lerp(robeColor, Color.black, 0.4f));
        var robe = CreatePrimitive(PrimitiveType.Capsule, visualRoot.transform, new Vector3(0f, 0.95f, 0f),
            new Vector3(0.78f, 0.9f, 0.78f), robeColor);
        var sash = CreatePrimitive(PrimitiveType.Cylinder, visualRoot.transform, new Vector3(0f, 0.85f, 0f),
            new Vector3(0.82f, 0.08f, 0.82f), trimColor);
        var hood = CreatePrimitive(PrimitiveType.Sphere, visualRoot.transform, new Vector3(0f, 1.55f, -0.05f),
            new Vector3(0.55f, 0.48f, 0.55f), hoodColor);
        var face = CreatePrimitive(PrimitiveType.Sphere, visualRoot.transform, new Vector3(0f, 1.48f, 0.12f),
            new Vector3(0.28f, 0.28f, 0.22f), new Color(0.95f, 0.8f, 0.7f));
        var shoulderL = CreatePrimitive(PrimitiveType.Sphere, visualRoot.transform, new Vector3(-0.42f, 1.2f, 0f),
            new Vector3(0.28f, 0.24f, 0.28f), trimColor);
        var shoulderR = CreatePrimitive(PrimitiveType.Sphere, visualRoot.transform, new Vector3(0.42f, 1.2f, 0f),
            new Vector3(0.28f, 0.24f, 0.28f), trimColor);
        var staff = CreatePrimitive(PrimitiveType.Cylinder, visualRoot.transform, new Vector3(0.55f, 1.1f, 0.15f),
            new Vector3(0.08f, 1.05f, 0.08f), staffColor);
        var orb = CreatePrimitive(PrimitiveType.Sphere, visualRoot.transform, new Vector3(0.55f, 2.05f, 0.15f),
            new Vector3(0.28f, 0.28f, 0.28f), gemColor);
        var cape = CreatePrimitive(PrimitiveType.Cube, visualRoot.transform, new Vector3(0f, 0.95f, -0.35f),
            new Vector3(0.7f, 1.1f, 0.12f), Color.Lerp(robeColor, Color.black, 0.2f));

        RemoveCollider(basePlate);
        RemoveCollider(robe);
        RemoveCollider(sash);
        RemoveCollider(hood);
        RemoveCollider(face);
        RemoveCollider(shoulderL);
        RemoveCollider(shoulderR);
        RemoveCollider(staff);
        RemoveCollider(orb);
        RemoveCollider(cape);

        CwslThreatLight.Ensure(orb.transform, gemColor, 3.5f, 2.2f, Vector3.zero);
    }

    private static GameObject CreatePrimitive(PrimitiveType type, Transform parent, Vector3 localPosition, Vector3 localScale, Color color)
    {
        var go = GameObject.CreatePrimitive(type);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localScale = localScale;
        ApplyColor(go, color);
        return go;
    }

    private static void ApplyColor(GameObject go, Color color)
    {
        var renderer = go.GetComponent<Renderer>();
        CwslMaterialUtil.ApplyColor(renderer, color);
    }

    private static void RemoveCollider(GameObject go)
    {
        var collider = go.GetComponent<Collider>();
        if (collider != null)
            Object.DestroyImmediate(collider);
    }
}
