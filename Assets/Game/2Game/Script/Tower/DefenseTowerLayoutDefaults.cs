using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 12시 상단 중앙(빨간 영역) 기본 타워 배치.
/// </summary>
public static class DefenseTowerLayoutDefaults
{
    /// <summary>상단 타워 구역 중심 (arenaCenter = 0 일 때)</summary>
    public static readonly Vector3 TowerZoneCenter = new(0f, 0f, 12f);

    /// <summary>10시 농장 월드 기준 대략적 중심 (arenaCenter = 0 일 때)</summary>
    public static readonly Vector3 FarmAreaCenter = new(-8.5f, 0f, 11.5f);

    public static List<TowerSpawnData> CreateNearFarmTowers() => CreateDefaultTowers();

    public static List<TowerSpawnData> CreateDefaultTowers()
    {
        return new List<TowerSpawnData>
        {
            Standard("Tower_North", new Color(0.2f, 0.45f, 1f),
                new Vector3(0f, 0f, 14f), 0f),
            Standard("Tower_NorthWest", new Color(0.2f, 0.85f, 0.35f),
                new Vector3(-3.5f, 0f, 12f), 315f),
            Standard("Tower_NorthEast", new Color(1f, 0.35f, 0.75f),
                new Vector3(3.5f, 0f, 12f), 45f),
            Standard("Tower_West", new Color(1f, 0.45f, 0.1f),
                new Vector3(-5f, 0f, 10f), 270f),
            Standard("Tower_East", new Color(0.35f, 0.65f, 1f),
                new Vector3(5f, 0f, 10f), 90f),
            new TowerSpawnData
            {
                towerName = "Tower_Meteor",
                kind = TowerKind.Meteor,
                color = new Color(0.92f, 0.28f, 0.08f),
                positionOffset = new Vector3(-1.5f, 0f, 13.5f),
                rotationY = 0f,
                scaleMultiplier = new Vector3(1.35f, 1.35f, 1.35f)
            },
            new TowerSpawnData
            {
                towerName = "Tower_ChainLightning",
                kind = TowerKind.ChainLightning,
                color = new Color(0.35f, 0.65f, 1f),
                positionOffset = new Vector3(1.5f, 0f, 13.5f),
                rotationY = 180f,
                scaleMultiplier = new Vector3(1.2f, 1.35f, 1.2f)
            },
            new TowerSpawnData
            {
                towerName = "Tower_Summon",
                kind = TowerKind.Summon,
                color = new Color(0.22f, 0.78f, 0.38f),
                positionOffset = new Vector3(0f, 0f, 10.5f),
                rotationY = 180f,
                scaleMultiplier = new Vector3(1.15f, 1.25f, 1.15f)
            }
        };
    }

    private static TowerSpawnData Standard(string name, Color color, Vector3 offset, float rotationY)
    {
        return new TowerSpawnData
        {
            towerName = name,
            kind = TowerKind.Standard,
            color = color,
            positionOffset = offset,
            rotationY = rotationY
        };
    }
}
