using UnityEngine;

/// <summary>아레나 기믹 구역 판정 (서버·클라 공용).</summary>
public static class CwslArenaZones
{
    private static readonly Vector3[] TrapPadCenters =
    {
        new(-12f, 0f, 12f),
        new(14f, 0f, -10f),
        new(-8f, 0f, -20f),
        new(18f, 0f, 8f),
        new(-28f, 0f, 0f),
        new(0f, 0f, 28f)
    };

    private static readonly CwslMonsterType[] TrapPadMonsterTypes =
    {
        CwslMonsterType.Suicide,
        CwslMonsterType.Ranged,
        CwslMonsterType.Suicide,
        CwslMonsterType.Ranged,
        CwslMonsterType.Suicide,
        CwslMonsterType.Ranged
    };

    private static readonly Vector3[] DonationPadCenters =
    {
        new(-6f, 0f, 22f),
        new(16f, 0f, -16f)
    };

    private static readonly Vector3[] BadGrassCenters =
    {
        new(-20f, 0f, 6f),
        new(8f, 0f, 14f),
        new(-14f, 0f, -12f),
        new(22f, 0f, -6f),
        new(-4f, 0f, -26f),
        new(26f, 0f, 14f),
        new(-30f, 0f, -18f),
        new(10f, 0f, 26f)
    };

    public static bool IsInFightZone(Vector3 position)
    {
        return IsInSquare(
            position,
            CwslGameConstants.FightZoneCenterX,
            CwslGameConstants.FightZoneCenterZ,
            CwslGameConstants.FightZoneHalfSize);
    }

    public static bool IsInBlackHoleZone(Vector3 position)
    {
        return IsInSquare(
            position,
            CwslGameConstants.BlackHoleZoneCenterX,
            CwslGameConstants.BlackHoleZoneCenterZ,
            CwslGameConstants.BlackHoleZoneHalfSize);
    }

    public static Vector3 GetBlackHoleCenter() =>
        new(CwslGameConstants.BlackHoleZoneCenterX, 0f, CwslGameConstants.BlackHoleZoneCenterZ);

    public static bool IsInKarmaHalfZone(Vector3 position)
    {
        return IsInSquare(
            position,
            CwslGameConstants.KarmaHalfZoneCenterX,
            CwslGameConstants.KarmaHalfZoneCenterZ,
            CwslGameConstants.KarmaHalfZoneHalfSize);
    }

    public static bool IsInTianyuan(Vector3 position)
    {
        return FlatDistance(position, Vector3.zero) <= CwslGameConstants.TianyuanRadius;
    }

    public static bool IsInPressConference(Vector3 position)
    {
        return FlatDistance(position, Vector3.zero) <= CwslGameConstants.PressConferenceRadius;
    }

    public static bool IsInFogVortex(Vector3 position)
    {
        return IsInCircle(position, new Vector3(CwslGameConstants.FogVortexCenterX, 0f, CwslGameConstants.FogVortexCenterZ), CwslGameConstants.FogVortexRadius)
               || IsInCircle(position, new Vector3(CwslGameConstants.FogVortexCenterX2, 0f, CwslGameConstants.FogVortexCenterZ2), CwslGameConstants.FogVortexRadius);
    }

    public static bool IsNearLighthouse(Vector3 position, int lighthouseIndex, out Vector3 lighthouseCenter)
    {
        lighthouseCenter = GetLighthouseCenter(lighthouseIndex);
        return FlatDistance(position, lighthouseCenter) <= CwslGameConstants.LighthouseRadius;
    }

    public static Vector3 GetLighthouseCenter(int index)
    {
        return index switch
        {
            0 => new Vector3(-34f, 0f, 34f),
            1 => new Vector3(34f, 0f, 34f),
            2 => new Vector3(-34f, 0f, -34f),
            3 => new Vector3(34f, 0f, -34f),
            4 => new Vector3(0f, 0f, 34f),
            5 => new Vector3(0f, 0f, -34f),
            6 => new Vector3(-34f, 0f, 0f),
            _ => new Vector3(34f, 0f, 0f)
        };
    }

    public static bool IsOnTrapPad(Vector3 position, int padIndex, out Vector3 padCenter)
    {
        padCenter = GetTrapPadCenter(padIndex);
        return FlatDistance(position, padCenter) <= CwslGameConstants.TrapPadRadius;
    }

    public static Vector3 GetTrapPadCenter(int index)
    {
        if (index < 0 || index >= TrapPadCenters.Length)
            return Vector3.zero;

        return TrapPadCenters[index];
    }

    public static CwslMonsterType GetTrapPadMonsterType(int index)
    {
        if (index < 0 || index >= TrapPadMonsterTypes.Length)
            return CwslMonsterType.Ranged;

        return TrapPadMonsterTypes[index];
    }

    public static string GetTrapPadLabel(int index)
    {
        return GetTrapPadMonsterType(index) switch
        {
            CwslMonsterType.Suicide => "자폭 함정",
            CwslMonsterType.Ranged => "원거리 함정",
            _ => "함정"
        };
    }

    public static string GetTrapPadSubtitle(int index)
    {
        return GetTrapPadMonsterType(index) switch
        {
            CwslMonsterType.Suicide => "밟으면 자폭 몬스터 출몰",
            CwslMonsterType.Ranged => "밟으면 원거리 몬스터 출몰",
            _ => "밟으면 몬스터 출몰"
        };
    }

    public static int GetTrapPadIndexAt(Vector3 position)
    {
        for (var i = 0; i < CwslGameConstants.TrapPadCount; i++)
        {
            if (IsOnTrapPad(position, i, out _))
                return i;
        }

        return -1;
    }

    public static bool IsOnDonationPad(Vector3 position, int padIndex, out Vector3 padCenter)
    {
        padCenter = GetDonationPadCenter(padIndex);
        return FlatDistance(position, padCenter) <= CwslGameConstants.DonationPadRadius;
    }

    public static Vector3 GetDonationPadCenter(int index)
    {
        if (index < 0 || index >= DonationPadCenters.Length)
            return Vector3.zero;

        return DonationPadCenters[index];
    }

    public static int GetDonationPadIndexAt(Vector3 position)
    {
        for (var i = 0; i < CwslGameConstants.DonationPadCount; i++)
        {
            if (IsOnDonationPad(position, i, out _))
                return i;
        }

        return -1;
    }

    public static bool IsInBadGrass(Vector3 position)
    {
        for (var i = 0; i < CwslGameConstants.BadGrassPatchCount; i++)
        {
            if (FlatDistance(position, BadGrassCenters[i]) <= CwslGameConstants.BadGrassPatchRadius)
                return true;
        }

        return false;
    }

    public static Vector3 GetBadGrassCenter(int index)
    {
        if (index < 0 || index >= BadGrassCenters.Length)
            return Vector3.zero;

        return BadGrassCenters[index];
    }

    public static bool IsInOffsideBossTerritory(Vector3 position)
    {
        return IsInFightZone(position) || IsInTianyuan(position);
    }

    public static float DistanceToLineSegmentXZ(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
    {
        var px = point.x;
        var pz = point.z;
        var ax = lineStart.x;
        var az = lineStart.z;
        var bx = lineEnd.x;
        var bz = lineEnd.z;

        var abx = bx - ax;
        var abz = bz - az;
        var lenSqr = abx * abx + abz * abz;
        if (lenSqr < 0.0001f)
            return FlatDistance(point, lineStart);

        var t = Mathf.Clamp01(((px - ax) * abx + (pz - az) * abz) / lenSqr);
        var cx = ax + abx * t;
        var cz = az + abz * t;
        var dx = px - cx;
        var dz = pz - cz;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    public static float GetMonsterSpeedMultiplier(Vector3 position)
    {
        return IsInFightZone(position) ? CwslGameConstants.FightZoneEnemySpeedMultiplier : 1f;
    }

    public static long ApplyKarmaMultiplier(Vector3 collectorPosition, long karmaAmount)
    {
        if (!IsInKarmaHalfZone(collectorPosition))
            return karmaAmount;

        return System.Math.Max(1L, (long)(karmaAmount * CwslGameConstants.KarmaHalfMultiplier));
    }

    private static bool IsInSquare(Vector3 position, float centerX, float centerZ, float halfSize)
    {
        return Mathf.Abs(position.x - centerX) <= halfSize
               && Mathf.Abs(position.z - centerZ) <= halfSize;
    }

    private static bool IsInCircle(Vector3 position, Vector3 center, float radius)
    {
        return FlatDistance(position, center) <= radius;
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        var flat = a - b;
        flat.y = 0f;
        return flat.magnitude;
    }
}
