using UnityEngine;

/// <summary>방패 탱커 스킬 VFX 위치·크기 계산.</summary>
public static class CwslTankShieldVfxUtil
{
    public static float GetShieldEffectScale(Transform playerRoot, bool empowered)
    {
        var shield = FindShield(playerRoot);
        if (shield != null)
            return Mathf.Max(1f, shield.lossyScale.x);

        return empowered ? CwslGameConstants.TankSkillEmpowerRadiusMultiplier : 1f;
    }

    public static Vector3 GetShieldFrontWorldPosition(Transform playerRoot, float forwardOffset = 0.2f)
    {
        var shield = FindShield(playerRoot);
        if (shield == null)
            return playerRoot.position + playerRoot.forward * 1.2f + Vector3.up * 0.9f;

        var scale = Mathf.Max(1f, shield.lossyScale.x);
        return shield.position + shield.forward * (0.42f * scale + forwardOffset);
    }

    public static Quaternion GetShieldFrontRotation(Transform playerRoot)
    {
        var shield = FindShield(playerRoot);
        return shield != null ? shield.rotation : playerRoot.rotation;
    }

    public const float ShieldBottomLocalY = -0.68f;
    public const float VisualGroundY = 0.05f;

    public static Vector3 GetShieldBottomWorldPoint(Transform shield)
    {
        if (shield == null)
            return Vector3.zero;

        var bottomLocal = new Vector3(0f, ShieldBottomLocalY * shield.lossyScale.y, 0.06f);
        var world = shield.TransformPoint(bottomLocal);
        world.y = VisualGroundY;
        return world;
    }

    public static Vector3 GetSlamGroundPoint(Transform playerRoot, bool empowered)
    {
        var shield = FindShield(playerRoot);
        if (shield != null)
            return GetShieldBottomWorldPoint(shield);

        var forward = playerRoot.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.forward;
        else
            forward.Normalize();

        var reach = 0.55f + (empowered ? 0.35f : 0f);
        var scale = GetShieldEffectScale(playerRoot, empowered);
        reach += Mathf.Max(0f, scale - 1f) * 0.14f;
        return playerRoot.position + forward * reach;
    }

    public static Quaternion GetSlamGroundRotation(Transform playerRoot)
    {
        var shield = FindShield(playerRoot);
        var forward = shield != null ? shield.forward : playerRoot.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
            forward = playerRoot.forward;

        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
            return Quaternion.identity;

        // ETFX BodySlam 계열 — 루트 X -90° (지면용).
        return Quaternion.LookRotation(forward.normalized, Vector3.up)
               * CwslEtfxVfxOrientation.GroundSlamRotationOffset;
    }

    public static float GetSlamGroundHitScale(Transform playerRoot, bool empowered)
    {
        var shieldScale = GetShieldEffectScale(playerRoot, empowered);
        return empowered
            ? shieldScale * CwslGameConstants.TankShieldSlamCartoonyVfxScale
            : shieldScale * CwslGameConstants.TankShieldSlamSoftVfxScale;
    }

    /// <summary>ETFX Sword 계열 — 돌진 파동 월드 정렬 (돌진 방향 + 프리팹 -90° X).</summary>
    public static Quaternion GetDashWaveWorldRotation(Vector3 dashDirection)
    {
        var flat = dashDirection;
        flat.y = 0f;
        if (flat.sqrMagnitude < 0.0001f)
            flat = Vector3.forward;
        else
            flat.Normalize();

        return Quaternion.LookRotation(flat, Vector3.up)
               * CwslEtfxVfxOrientation.ShieldDashWaveWorldRotationOffset;
    }

    public static Quaternion GetDashWaveLocalRotation() =>
        CwslEtfxVfxOrientation.ShieldDashWaveAttachLocalRotation;

    public static Vector3 GetDashWaveSpawnWorldPosition(Transform visualOrPlayerRoot, Vector3 dashDirection, float shieldScale)
    {
        var shield = FindShield(visualOrPlayerRoot);
        var flat = dashDirection;
        flat.y = 0f;
        if (flat.sqrMagnitude < 0.0001f)
            flat = shield != null ? shield.forward : visualOrPlayerRoot.forward;
        else
            flat.Normalize();

        flat.y = 0f;
        if (flat.sqrMagnitude < 0.0001f)
            flat = Vector3.forward;
        else
            flat.Normalize();

        var forwardOffset = 0.42f * shieldScale + 0.05f;
        if (shield != null)
            return shield.position + flat * forwardOffset;

        return visualOrPlayerRoot.position + flat * 1.2f + Vector3.up * 0.9f;
    }

    public static Quaternion GetShieldWhirlwindAttachLocalRotation() =>
        CwslEtfxVfxOrientation.ShieldWhirlwindAttachRotation;

    public static Vector3 GetDashWaveLocalOffset(float shieldScale) =>
        new Vector3(0f, 0f, 0.48f + Mathf.Max(0f, shieldScale - 1f) * 0.08f);

    public static Transform FindShieldForVfx(Transform root) => FindShield(root);

    private static Transform FindShield(Transform root)
    {
        if (root == null)
            return null;

        var direct = root.Find("Shield");
        if (direct != null)
            return direct;

        var visual = root.Find("Visual");
        return visual != null ? visual.Find("Shield") : null;
    }
}
