using UnityEngine;

public static class CwslArenaUtility
{
    /// <summary>방어 모드는 맵 끝, 기존 아레나는 ArenaHalfExtent.</summary>
    public static float GetPlayHalfExtent()
    {
        return CwslGameConstants.UseDefenseMode
            ? CwslGameConstants.ArenaMapHalfExtent
            : CwslGameConstants.ArenaHalfExtent;
    }

    public static float GetMapEdgeHalfExtent(float inset = 0f)
    {
        return CwslGameConstants.ArenaMapHalfExtent - inset;
    }

    /// <summary>월드 XZ 축 정렬 사각형 거리 — max(|dx|, |dz|).</summary>
    public static float GetFlatRectDistance(Vector3 from, Vector3 to)
    {
        var dx = Mathf.Abs(to.x - from.x);
        var dz = Mathf.Abs(to.z - from.z);
        return Mathf.Max(dx, dz);
    }

    public static bool IsInsideFlatRect(Vector3 center, Vector3 point, float halfExtent)
    {
        return Mathf.Abs(point.x - center.x) <= halfExtent
               && Mathf.Abs(point.z - center.z) <= halfExtent;
    }

    public static bool IsInMonsterSpawnBand(Vector3 point)
    {
        if (CwslGameConstants.UseDefenseMode)
            return IsOnMapEdge(point, CwslGameConstants.MapEdgeSpawnInset + 0.8f);

        var inner = CwslGameConstants.MonsterSpawnInnerHalfExtent;
        var outer = CwslGameConstants.MonsterSpawnOuterHalfExtent;
        return IsInsideFlatRect(Vector3.zero, point, outer)
               && !IsInsideFlatRect(Vector3.zero, point, inner);
    }

    public static bool IsOnMapEdge(Vector3 point, float inset = -1f)
    {
        if (inset < 0f)
            inset = CwslGameConstants.MapEdgeSpawnInset;

        var edge = GetMapEdgeHalfExtent(inset);
        var absX = Mathf.Abs(point.x);
        var absZ = Mathf.Abs(point.z);
        var onXWall = absX >= edge - 0.35f && absZ <= edge;
        var onZWall = absZ >= edge - 0.35f && absX <= edge;
        return onXWall || onZWall;
    }

    /// <summary>맵 전체 사각형 안 임의 위치 (기믹·함정 등).</summary>
    public static Vector3 GetRandomSpawnPosition()
    {
        var extent = GetPlayHalfExtent();
        var x = Random.Range(-extent, extent);
        var z = Random.Range(-extent, extent);
        return new Vector3(x, CwslGameConstants.SpawnHeight, z);
    }

    /// <summary>맵 네 변 벽에 붙은 스폰 위치.</summary>
    public static Vector3 GetRandomMapEdgeSpawnPosition(float inset = -1f)
    {
        if (inset < 0f)
            inset = CwslGameConstants.MapEdgeSpawnInset;

        var edge = GetMapEdgeHalfExtent(inset);
        var along = Random.Range(-edge * 0.94f, edge * 0.94f);

        return Random.Range(0, 4) switch
        {
            0 => new Vector3(edge, CwslGameConstants.SpawnHeight, along),
            1 => new Vector3(-edge, CwslGameConstants.SpawnHeight, along),
            2 => new Vector3(along, CwslGameConstants.SpawnHeight, edge),
            _ => new Vector3(along, CwslGameConstants.SpawnHeight, -edge)
        };
    }

    /// <summary>방어 모드: 맵 벽. 기존 아레나: 안쪽 금지 링.</summary>
    public static Vector3 GetRandomMonsterSpawnPosition()
    {
        if (CwslGameConstants.UseDefenseMode)
            return GetRandomMapEdgeSpawnPosition();

        var inner = CwslGameConstants.MonsterSpawnInnerHalfExtent;
        var outer = CwslGameConstants.MonsterSpawnOuterHalfExtent;

        for (var attempt = 0; attempt < 48; attempt++)
        {
            var x = Random.Range(-outer, outer);
            var z = Random.Range(-outer, outer);
            if (!IsInsideFlatRect(Vector3.zero, new Vector3(x, 0f, z), inner))
                return new Vector3(x, CwslGameConstants.SpawnHeight, z);
        }

        return GetRandomMonsterSpawnPositionOnEdge(inner, outer);
    }

    private static Vector3 GetRandomMonsterSpawnPositionOnEdge(float inner, float outer)
    {
        return Random.Range(0, 4) switch
        {
            0 => new Vector3(Random.Range(inner, outer), CwslGameConstants.SpawnHeight, Random.Range(-outer, outer)),
            1 => new Vector3(Random.Range(-outer, -inner), CwslGameConstants.SpawnHeight, Random.Range(-outer, outer)),
            2 => new Vector3(Random.Range(-outer, outer), CwslGameConstants.SpawnHeight, Random.Range(inner, outer)),
            _ => new Vector3(Random.Range(-outer, outer), CwslGameConstants.SpawnHeight, Random.Range(-outer, -inner))
        };
    }

    public static Vector3 ClampToArena(Vector3 position)
    {
        var extent = GetPlayHalfExtent();
        position.x = Mathf.Clamp(position.x, -extent, extent);
        position.z = Mathf.Clamp(position.z, -extent, extent);
        return position;
    }

    /// <summary>유닛 몸체 반경을 고려해 맵 사각형 안으로 제한.</summary>
    public static Vector3 ClampToPlayArea(Vector3 position, float bodyRadius = 0f)
    {
        var extent = Mathf.Max(0.5f, GetPlayHalfExtent() - bodyRadius);
        position.x = Mathf.Clamp(position.x, -extent, extent);
        position.z = Mathf.Clamp(position.z, -extent, extent);
        return position;
    }

    public static float GetMapEdgePerimeter(float inset = -1f)
    {
        if (inset < 0f)
            inset = CwslGameConstants.MapEdgeSpawnInset;

        var edge = GetMapEdgeHalfExtent(inset);
        return edge * 8f;
    }

    /// <summary>맵 사각형 외곽을 시계 방향으로 도는 위치.</summary>
    public static Vector3 GetMapEdgeOrbitPosition(float distanceAlongPerimeter, float inset = -1f)
    {
        if (inset < 0f)
            inset = CwslGameConstants.MapEdgeSpawnInset;

        var edge = GetMapEdgeHalfExtent(inset);
        var sideLength = edge * 2f;
        var perimeter = sideLength * 4f;
        if (perimeter <= 0.001f)
            return new Vector3(edge, CwslGameConstants.SpawnHeight, -edge);

        var t = Mathf.Repeat(distanceAlongPerimeter, perimeter);

        if (t < sideLength)
            return new Vector3(edge, CwslGameConstants.SpawnHeight, -edge + t);

        t -= sideLength;
        if (t < sideLength)
            return new Vector3(edge - t, CwslGameConstants.SpawnHeight, edge);

        t -= sideLength;
        if (t < sideLength)
            return new Vector3(-edge, CwslGameConstants.SpawnHeight, edge - t);

        t -= sideLength;
        return new Vector3(-edge + t, CwslGameConstants.SpawnHeight, -edge);
    }
}
