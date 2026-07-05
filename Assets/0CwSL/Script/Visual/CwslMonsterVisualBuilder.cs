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

        AddWalkLegs(visualRoot.transform, trimColor);
        visualRoot.AddComponent<CwslPlayerLegWalkVisual>();
        visualRoot.AddComponent<CwslPlayerShieldWalkVisual>();
    }

    public static void BuildMissileTankPlayer(Transform root, Color accentColor)
    {
        var visualRoot = new GameObject("Visual");
        visualRoot.transform.SetParent(root, false);
        visualRoot.AddComponent<CwslPlayerGunShootVisual>();

        var armorColor = Color.Lerp(accentColor, new Color(0.22f, 0.28f, 0.18f), 0.35f);
        var trimColor = Color.Lerp(armorColor, Color.black, 0.32f);
        var leatherColor = new Color(0.45f, 0.28f, 0.12f);
        var gunMetal = new Color(0.58f, 0.6f, 0.66f);

        var basePlate = CreatePrimitive(PrimitiveType.Cylinder, visualRoot.transform, new Vector3(0f, 0.08f, 0f),
            new Vector3(1.02f, 0.06f, 1.02f), trimColor);
        var body = CreatePrimitive(PrimitiveType.Capsule, visualRoot.transform, new Vector3(0f, 0.92f, -0.06f),
            new Vector3(0.78f, 0.86f, 0.78f), armorColor);
        var chestPlate = CreatePrimitive(PrimitiveType.Cube, visualRoot.transform, new Vector3(0f, 1.0f, 0.1f),
            new Vector3(0.58f, 0.48f, 0.24f), Color.Lerp(armorColor, Color.white, 0.18f));
        var shoulderL = CreatePrimitive(PrimitiveType.Sphere, visualRoot.transform, new Vector3(-0.44f, 1.16f, -0.02f),
            new Vector3(0.34f, 0.28f, 0.34f), trimColor);
        var shoulderR = CreatePrimitive(PrimitiveType.Sphere, visualRoot.transform, new Vector3(0.44f, 1.16f, -0.02f),
            new Vector3(0.34f, 0.28f, 0.34f), trimColor);
        var hood = CreatePrimitive(PrimitiveType.Sphere, visualRoot.transform, new Vector3(0f, 1.58f, -0.04f),
            new Vector3(0.46f, 0.4f, 0.46f), trimColor);
        var face = CreatePrimitive(PrimitiveType.Sphere, visualRoot.transform, new Vector3(0f, 1.5f, 0.12f),
            new Vector3(0.24f, 0.24f, 0.2f), new Color(0.92f, 0.78f, 0.66f));
        var cape = CreatePrimitive(PrimitiveType.Cube, visualRoot.transform, new Vector3(0f, 0.95f, -0.38f),
            new Vector3(0.62f, 0.82f, 0.12f), Color.Lerp(armorColor, accentColor, 0.2f));

        var armRPivot = new GameObject("ArmRPivot");
        armRPivot.transform.SetParent(visualRoot.transform, false);
        armRPivot.transform.localPosition = new Vector3(0.4f, 1.12f, 0.06f);
        armRPivot.transform.localRotation = Quaternion.Euler(8f, 12f, -6f);
        var upperArm = CreatePrimitive(PrimitiveType.Cube, armRPivot.transform, new Vector3(0.1f, -0.1f, 0.06f),
            new Vector3(0.16f, 0.34f, 0.16f), armorColor);
        var foreArm = CreatePrimitive(PrimitiveType.Cube, armRPivot.transform, new Vector3(0.2f, -0.28f, 0.14f),
            new Vector3(0.12f, 0.24f, 0.12f), new Color(0.92f, 0.78f, 0.66f));

        var armLPivot = new GameObject("ArmLPivot");
        armLPivot.transform.SetParent(visualRoot.transform, false);
        armLPivot.transform.localPosition = new Vector3(-0.36f, 1.12f, 0.04f);
        armLPivot.transform.localRotation = Quaternion.Euler(8f, -14f, 8f);
        var leftUpperArm = CreatePrimitive(PrimitiveType.Cube, armLPivot.transform, new Vector3(-0.08f, -0.1f, 0.04f),
            new Vector3(0.15f, 0.32f, 0.15f), armorColor);
        var leftForeArm = CreatePrimitive(PrimitiveType.Cube, armLPivot.transform, new Vector3(-0.16f, -0.26f, 0.12f),
            new Vector3(0.12f, 0.24f, 0.12f), new Color(0.92f, 0.78f, 0.66f));

        BuildGunOnArm(
            armRPivot.transform,
            "BowAimPivot",
            "CannonPivot",
            new Vector3(0.24f, -0.34f, 0.2f),
            gunMetal,
            leatherColor,
            isLeftArm: false);
        BuildGunOnArm(
            armLPivot.transform,
            "BowAimPivotL",
            "CannonPivotL",
            new Vector3(-0.18f, -0.28f, 0.18f),
            gunMetal,
            leatherColor,
            isLeftArm: true);

        RemoveCollider(upperArm);
        RemoveCollider(foreArm);
        RemoveCollider(leftUpperArm);
        RemoveCollider(leftForeArm);
        RemoveCollider(basePlate);
        RemoveCollider(body);
        RemoveCollider(chestPlate);
        RemoveCollider(shoulderL);
        RemoveCollider(shoulderR);
        RemoveCollider(hood);
        RemoveCollider(face);
        RemoveCollider(cape);

        AddWalkLegs(visualRoot.transform, trimColor);
        visualRoot.AddComponent<CwslPlayerLegWalkVisual>();
    }

    private static void BuildGunOnArm(
        Transform armPivot,
        string aimPivotName,
        string cannonPivotName,
        Vector3 aimLocalPosition,
        Color gunMetal,
        Color gripColor,
        bool isLeftArm)
    {
        var aimPivot = new GameObject(aimPivotName);
        aimPivot.transform.SetParent(armPivot, false);
        aimPivot.transform.localPosition = aimLocalPosition;
        aimPivot.transform.localRotation = Quaternion.identity;

        var cannonPivot = new GameObject(cannonPivotName);
        cannonPivot.transform.SetParent(aimPivot.transform, false);
        cannonPivot.transform.localPosition = Vector3.zero;
        cannonPivot.transform.localRotation = Quaternion.Euler(8f, 0f, isLeftArm ? 4f : -4f);

        // ㄱ자 권총: 슬라이드는 앞(+Z), 손잡이는 뒤에서 아래(-Y)로 꺾임
        var slide = CreatePrimitive(PrimitiveType.Cube, cannonPivot.transform, new Vector3(0f, 0.02f, 0.08f),
            new Vector3(0.11f, 0.07f, 0.2f), gunMetal);
        var barrel = CreatePrimitive(PrimitiveType.Cylinder, cannonPivot.transform, new Vector3(0f, 0.02f, 0.24f),
            new Vector3(0.05f, 0.07f, 0.05f), Color.Lerp(gunMetal, Color.black, 0.2f));
        barrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var gripOffsetX = isLeftArm ? 0.015f : -0.015f;
        var grip = CreatePrimitive(PrimitiveType.Cube, cannonPivot.transform, new Vector3(gripOffsetX, -0.11f, -0.03f),
            new Vector3(0.07f, 0.15f, 0.09f), gripColor);

        var muzzle = new GameObject("Muzzle");
        muzzle.transform.SetParent(cannonPivot.transform, false);
        muzzle.transform.localPosition = new Vector3(0f, 0.02f, 0.34f);
        muzzle.transform.localRotation = Quaternion.identity;

        RemoveCollider(slide);
        RemoveCollider(barrel);
        RemoveCollider(grip);
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
        var robe = CreatePrimitive(PrimitiveType.Capsule, visualRoot.transform, new Vector3(0f, 0.72f, 0f),
            new Vector3(0.78f, 0.72f, 0.78f), robeColor);

        var torsoPivot = new GameObject("TorsoPivot");
        torsoPivot.transform.SetParent(visualRoot.transform, false);
        torsoPivot.transform.localPosition = new Vector3(0f, 0.95f, 0f);

        var sash = CreatePrimitive(PrimitiveType.Cylinder, torsoPivot.transform, new Vector3(0f, -0.08f, 0f),
            new Vector3(0.82f, 0.08f, 0.82f), trimColor);
        var hood = CreatePrimitive(PrimitiveType.Sphere, torsoPivot.transform, new Vector3(0f, 0.62f, -0.05f),
            new Vector3(0.55f, 0.48f, 0.55f), hoodColor);
        var face = CreatePrimitive(PrimitiveType.Sphere, torsoPivot.transform, new Vector3(0f, 0.55f, 0.12f),
            new Vector3(0.28f, 0.28f, 0.22f), new Color(0.95f, 0.8f, 0.7f));
        var shoulderL = CreatePrimitive(PrimitiveType.Sphere, torsoPivot.transform, new Vector3(-0.42f, 0.27f, 0f),
            new Vector3(0.28f, 0.24f, 0.28f), trimColor);
        var shoulderR = CreatePrimitive(PrimitiveType.Sphere, torsoPivot.transform, new Vector3(0.42f, 0.27f, 0f),
            new Vector3(0.28f, 0.24f, 0.28f), trimColor);
        var cape = CreatePrimitive(PrimitiveType.Cube, torsoPivot.transform, new Vector3(0f, 0.02f, -0.35f),
            new Vector3(0.7f, 1.1f, 0.12f), Color.Lerp(robeColor, Color.black, 0.2f));

        var castArmPivot = new GameObject("CastArmPivot");
        castArmPivot.transform.SetParent(visualRoot.transform, false);
        castArmPivot.transform.localPosition = new Vector3(-0.36f, 1.02f, 0.12f);
        castArmPivot.transform.localRotation = Quaternion.Euler(10f, -18f, 0f);
        var castArm = CreatePrimitive(PrimitiveType.Cube, castArmPivot.transform, new Vector3(0.08f, -0.08f, 0.04f),
            new Vector3(0.14f, 0.32f, 0.14f), new Color(0.95f, 0.8f, 0.7f));

        var staffPivot = new GameObject("StaffPivot");
        staffPivot.transform.SetParent(castArmPivot.transform, false);
        staffPivot.transform.localPosition = new Vector3(0.16f, -0.18f, 0.08f);
        staffPivot.transform.localRotation = Quaternion.Euler(8f, -12f, 0f);

        var staff = CreatePrimitive(PrimitiveType.Cylinder, staffPivot.transform, new Vector3(0f, 0.58f, 0f),
            new Vector3(0.08f, 1.05f, 0.08f), staffColor);
        var orb = CreatePrimitive(PrimitiveType.Sphere, staffPivot.transform, new Vector3(0f, 1.18f, 0f),
            new Vector3(0.28f, 0.28f, 0.28f), gemColor);

        RemoveCollider(basePlate);
        RemoveCollider(robe);
        RemoveCollider(sash);
        RemoveCollider(hood);
        RemoveCollider(face);
        RemoveCollider(shoulderL);
        RemoveCollider(shoulderR);
        RemoveCollider(cape);
        RemoveCollider(castArm);
        RemoveCollider(staff);
        RemoveCollider(orb);

        CwslThreatLight.Ensure(orb.transform, gemColor, 3.5f, 2.2f, Vector3.zero);

        AddWalkLegs(visualRoot.transform, Color.Lerp(robeColor, Color.black, 0.35f));
        visualRoot.AddComponent<CwslPlayerLegWalkVisual>();
        visualRoot.AddComponent<CwslPlayerStaffCastVisual>();
    }

    public static void BuildMomentumRammerPlayer(Transform root, Color accentColor)
    {
        var visualRoot = new GameObject("Visual");
        visualRoot.transform.SetParent(root, false);

        var horseColor = Color.Lerp(accentColor, new Color(0.38f, 0.24f, 0.12f), 0.48f);
        var maneColor = Color.Lerp(accentColor, Color.black, 0.22f);
        var hoofColor = new Color(0.18f, 0.14f, 0.12f);
        var riderColor = Color.Lerp(accentColor, new Color(0.95f, 0.55f, 0.15f), 0.35f);
        var riderTrim = Color.Lerp(riderColor, Color.black, 0.28f);

        var horseRoot = new GameObject("HorseRoot");
        horseRoot.transform.SetParent(visualRoot.transform, false);

        var shadowPlate = CreatePrimitive(PrimitiveType.Cylinder, horseRoot.transform, new Vector3(0f, 0.03f, 0f),
            new Vector3(1.28f, 0.03f, 1.55f), new Color(0.08f, 0.08f, 0.1f, 0.65f));

        var horseBody = CreatePrimitive(PrimitiveType.Capsule, horseRoot.transform, new Vector3(0f, 0.78f, 0f),
            new Vector3(0.95f, 0.52f, 0.42f), horseColor);
        horseBody.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        var horseHead = CreatePrimitive(PrimitiveType.Cube, horseRoot.transform, new Vector3(0f, 1.02f, 0.48f),
            new Vector3(0.34f, 0.32f, 0.42f), horseColor);
        var horseNeck = CreatePrimitive(PrimitiveType.Cylinder, horseRoot.transform, new Vector3(0f, 0.98f, 0.24f),
            new Vector3(0.22f, 0.18f, 0.22f), horseColor);
        horseNeck.transform.localRotation = Quaternion.Euler(58f, 0f, 0f);
        var mane = CreatePrimitive(PrimitiveType.Cube, horseRoot.transform, new Vector3(0f, 1.08f, 0.08f),
            new Vector3(0.08f, 0.28f, 0.42f), maneColor);
        var tail = CreatePrimitive(PrimitiveType.Cube, horseRoot.transform, new Vector3(0f, 0.82f, -0.52f),
            new Vector3(0.08f, 0.34f, 0.24f), maneColor);

        var bladeColor = Color.Lerp(accentColor, new Color(1f, 0.92f, 0.35f), 0.55f);
        var bladeEdgeColor = Color.Lerp(bladeColor, Color.white, 0.35f);
        var horseBodyBladeSpin = new GameObject("HorseBodyBladeSpin");
        horseBodyBladeSpin.transform.SetParent(horseRoot.transform, false);
        horseBodyBladeSpin.transform.localPosition = new Vector3(0f, 0.56f, 0f);
        var bladePlate = CreatePrimitive(PrimitiveType.Cube, horseBodyBladeSpin.transform, Vector3.zero,
            new Vector3(1.02f, 0.14f, 1.02f), bladeColor);
        bladePlate.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
        var bladeTipN = CreatePrimitive(PrimitiveType.Cube, horseBodyBladeSpin.transform, new Vector3(0f, 0f, 0.36f),
            new Vector3(0.14f, 0.1f, 0.42f), bladeEdgeColor);
        var bladeTipS = CreatePrimitive(PrimitiveType.Cube, horseBodyBladeSpin.transform, new Vector3(0f, 0f, -0.36f),
            new Vector3(0.14f, 0.1f, 0.42f), bladeEdgeColor);
        var bladeTipE = CreatePrimitive(PrimitiveType.Cube, horseBodyBladeSpin.transform, new Vector3(0.36f, 0f, 0f),
            new Vector3(0.42f, 0.1f, 0.14f), bladeEdgeColor);
        var bladeTipW = CreatePrimitive(PrimitiveType.Cube, horseBodyBladeSpin.transform, new Vector3(-0.36f, 0f, 0f),
            new Vector3(0.42f, 0.1f, 0.14f), bladeEdgeColor);

        AddHorseLeg(horseRoot.transform, "HorseLegFL", new Vector3(-0.26f, 0.42f, 0.34f), horseColor, hoofColor);
        AddHorseLeg(horseRoot.transform, "HorseLegFR", new Vector3(0.26f, 0.42f, 0.34f), horseColor, hoofColor);
        AddHorseLeg(horseRoot.transform, "HorseLegBL", new Vector3(-0.26f, 0.42f, -0.34f), horseColor, hoofColor);
        AddHorseLeg(horseRoot.transform, "HorseLegBR", new Vector3(0.26f, 0.42f, -0.34f), horseColor, hoofColor);

        var riderPivot = new GameObject("RiderPivot");
        riderPivot.transform.SetParent(horseRoot.transform, false);
        riderPivot.transform.localPosition = new Vector3(0f, 1.08f, -0.04f);

        var skinColor = Color.Lerp(new Color(0.92f, 0.72f, 0.58f), riderColor, 0.35f);
        var hairColor = Color.Lerp(riderTrim, new Color(0.15f, 0.1f, 0.08f), 0.4f);

        var riderBody = CreatePrimitive(PrimitiveType.Capsule, riderPivot.transform, new Vector3(0f, 0.12f, 0f),
            new Vector3(0.5f, 0.36f, 0.36f), riderColor);
        var riderCape = CreatePrimitive(PrimitiveType.Cube, riderPivot.transform, new Vector3(0f, 0.1f, -0.16f),
            new Vector3(0.44f, 0.42f, 0.08f), Color.Lerp(riderColor, accentColor, 0.25f));

        var headPivot = new GameObject("HeadPivot");
        headPivot.transform.SetParent(riderPivot.transform, false);
        headPivot.transform.localPosition = new Vector3(0f, 0.48f, 0.02f);

        var riderNeck = CreatePrimitive(PrimitiveType.Cylinder, headPivot.transform, new Vector3(0f, -0.06f, 0f),
            new Vector3(0.14f, 0.1f, 0.14f), skinColor);
        var riderHead = CreatePrimitive(PrimitiveType.Sphere, headPivot.transform, new Vector3(0f, 0.14f, 0.02f),
            new Vector3(0.32f, 0.34f, 0.3f), skinColor);
        var riderFace = CreatePrimitive(PrimitiveType.Cube, headPivot.transform, new Vector3(0f, 0.1f, 0.15f),
            new Vector3(0.14f, 0.09f, 0.06f), Color.Lerp(skinColor, new Color(0.75f, 0.5f, 0.42f), 0.35f));
        var riderHair = CreatePrimitive(PrimitiveType.Cube, headPivot.transform, new Vector3(0f, 0.24f, -0.05f),
            new Vector3(0.34f, 0.14f, 0.3f), hairColor);
        var riderEyeL = CreatePrimitive(PrimitiveType.Cube, headPivot.transform, new Vector3(-0.08f, 0.14f, 0.14f),
            new Vector3(0.05f, 0.04f, 0.03f), new Color(0.12f, 0.1f, 0.1f));
        var riderEyeR = CreatePrimitive(PrimitiveType.Cube, headPivot.transform, new Vector3(0.08f, 0.14f, 0.14f),
            new Vector3(0.05f, 0.04f, 0.03f), new Color(0.12f, 0.1f, 0.1f));

        RemoveCollider(shadowPlate);
        RemoveCollider(horseBody);
        RemoveCollider(horseHead);
        RemoveCollider(horseNeck);
        RemoveCollider(mane);
        RemoveCollider(tail);
        RemoveCollider(bladePlate);
        RemoveCollider(bladeTipN);
        RemoveCollider(bladeTipS);
        RemoveCollider(bladeTipE);
        RemoveCollider(bladeTipW);
        RemoveCollider(riderBody);
        RemoveCollider(riderNeck);
        RemoveCollider(riderHead);
        RemoveCollider(riderFace);
        RemoveCollider(riderHair);
        RemoveCollider(riderEyeL);
        RemoveCollider(riderEyeR);
        RemoveCollider(riderCape);

        visualRoot.AddComponent<CwslPlayerHorseRideVisual>();
        visualRoot.AddComponent<CwslPlayerRammerTopSpinVisual>();
        visualRoot.AddComponent<CwslPlayerRammerStunVisual>();
        visualRoot.AddComponent<CwslPlayerRammerBrakeVisual>();
    }

    public static void BuildCrowdGathererPlayer(Transform root, Color accentColor)
    {
        var visualRoot = new GameObject("Visual");
        visualRoot.transform.SetParent(root, false);

        var robeColor = Color.Lerp(accentColor, new Color(0.45f, 0.18f, 0.82f), 0.42f);
        var trimColor = Color.Lerp(robeColor, Color.black, 0.35f);
        var threadColor = Color.Lerp(accentColor, new Color(0.85f, 0.55f, 1f), 0.55f);
        var skinColor = new Color(0.92f, 0.74f, 0.6f);

        var basePlate = CreatePrimitive(PrimitiveType.Cylinder, visualRoot.transform, new Vector3(0f, 0.06f, 0f),
            new Vector3(0.95f, 0.05f, 0.95f), trimColor);
        var body = CreatePrimitive(PrimitiveType.Capsule, visualRoot.transform, new Vector3(0f, 0.92f, 0f),
            new Vector3(0.72f, 0.82f, 0.72f), robeColor);
        var sash = CreatePrimitive(PrimitiveType.Cube, visualRoot.transform, new Vector3(0f, 0.92f, 0.14f),
            new Vector3(0.58f, 0.16f, 0.18f), threadColor);

        var head = CreatePrimitive(PrimitiveType.Sphere, visualRoot.transform, new Vector3(0f, 1.52f, 0f),
            new Vector3(0.42f, 0.42f, 0.42f), skinColor);
        var hood = CreatePrimitive(PrimitiveType.Cube, visualRoot.transform, new Vector3(0f, 1.62f, -0.06f),
            new Vector3(0.5f, 0.24f, 0.42f), trimColor);

        var spoolRoot = new GameObject("ThreadSpool");
        spoolRoot.transform.SetParent(visualRoot.transform, false);
        spoolRoot.transform.localPosition = new Vector3(0f, 1.05f, -0.28f);
        var spool = CreatePrimitive(PrimitiveType.Cylinder, spoolRoot.transform, Vector3.zero,
            new Vector3(0.42f, 0.16f, 0.42f), threadColor);
        spool.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var spoolCore = CreatePrimitive(PrimitiveType.Cylinder, spoolRoot.transform, Vector3.zero,
            new Vector3(0.18f, 0.22f, 0.18f), trimColor);
        spoolCore.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        var armL = CreatePrimitive(PrimitiveType.Capsule, visualRoot.transform, new Vector3(-0.42f, 0.98f, 0.08f),
            new Vector3(0.16f, 0.34f, 0.16f), robeColor);
        armL.transform.localRotation = Quaternion.Euler(0f, 0f, 78f);
        var armR = CreatePrimitive(PrimitiveType.Capsule, visualRoot.transform, new Vector3(0.42f, 0.98f, 0.08f),
            new Vector3(0.16f, 0.34f, 0.16f), robeColor);
        armR.transform.localRotation = Quaternion.Euler(0f, 0f, -78f);

        var threadLine = CreatePrimitive(PrimitiveType.Cube, visualRoot.transform, new Vector3(0.18f, 0.72f, 0.32f),
            new Vector3(0.04f, 0.04f, 0.72f), threadColor);
        threadLine.transform.localRotation = Quaternion.Euler(0f, 24f, 0f);

        RemoveCollider(basePlate);
        RemoveCollider(body);
        RemoveCollider(sash);
        RemoveCollider(head);
        RemoveCollider(hood);
        RemoveCollider(spool);
        RemoveCollider(spoolCore);
        RemoveCollider(armL);
        RemoveCollider(armR);
        RemoveCollider(threadLine);

        CwslThreatLight.Ensure(spoolRoot.transform, threadColor, 2.8f, 1.8f, Vector3.zero);
        AddWalkLegs(visualRoot.transform, Color.Lerp(robeColor, Color.black, 0.35f));
        visualRoot.AddComponent<CwslPlayerLegWalkVisual>();
    }

    private static void AddHorseLeg(Transform horseRoot, string legName, Vector3 localPosition, Color legColor, Color hoofColor)
    {
        var legPivot = new GameObject(legName);
        legPivot.transform.SetParent(horseRoot, false);
        legPivot.transform.localPosition = localPosition;

        var legMesh = CreatePrimitive(PrimitiveType.Cube, legPivot.transform, new Vector3(0f, -0.16f, 0f),
            new Vector3(0.14f, 0.32f, 0.14f), legColor);
        var hoof = CreatePrimitive(PrimitiveType.Cube, legPivot.transform, new Vector3(0f, -0.34f, 0.02f),
            new Vector3(0.12f, 0.08f, 0.16f), hoofColor);

        RemoveCollider(legMesh);
        RemoveCollider(hoof);
    }

    private static void AddWalkLegs(Transform visualRoot, Color legColor)
    {
        var legL = new GameObject("LegL");
        legL.transform.SetParent(visualRoot, false);
        legL.transform.localPosition = new Vector3(-0.22f, 0.42f, 0f);
        var legLMesh = CreatePrimitive(PrimitiveType.Cube, legL.transform, new Vector3(0f, -0.2f, 0f),
            new Vector3(0.22f, 0.4f, 0.22f), legColor);

        var legR = new GameObject("LegR");
        legR.transform.SetParent(visualRoot, false);
        legR.transform.localPosition = new Vector3(0.22f, 0.42f, 0f);
        var legRMesh = CreatePrimitive(PrimitiveType.Cube, legR.transform, new Vector3(0f, -0.2f, 0f),
            new Vector3(0.22f, 0.4f, 0.22f), legColor);

        RemoveCollider(legLMesh);
        RemoveCollider(legRMesh);
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
