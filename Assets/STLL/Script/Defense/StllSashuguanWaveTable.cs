using System.Collections.Generic;
using UnityEngine;

public readonly struct StllWaveSpawnEntry
{
    public readonly StllEnemyKind Kind;
    public readonly int Count;
    public readonly float IntervalSeconds;
    public readonly int SpawnDirection;
    public readonly bool SpawnMiniBoss;

    public StllWaveSpawnEntry(StllEnemyKind kind, int count, float intervalSeconds, int spawnDirection, bool spawnMiniBoss = false)
    {
        Kind = kind;
        Count = count;
        IntervalSeconds = intervalSeconds;
        SpawnDirection = spawnDirection;
        SpawnMiniBoss = spawnMiniBoss;
    }
}

public readonly struct StllWaveDefinition
{
    public readonly string Name;
    public readonly float StartSeconds;
    public readonly IReadOnlyList<StllWaveSpawnEntry> Spawns;

    public StllWaveDefinition(string name, float startSeconds, IReadOnlyList<StllWaveSpawnEntry> spawns)
    {
        Name = name;
        StartSeconds = startSeconds;
        Spawns = spawns;
    }
}

/// <summary>사수관 EA 웨이브 테이블 (기획서 W1~W6 + 화웅).</summary>
public static class StllSashuguanWaveTable
{
    public const float StageDurationSeconds = 480f;

    public static IReadOnlyList<StllWaveDefinition> Waves { get; } = new List<StllWaveDefinition>
    {
        new("W1 첫 포성", 0f, new List<StllWaveSpawnEntry>
        {
            new(StllEnemyKind.Grunt, 12, 4.5f, StllPrimitiveMapBuilder.SpawnNorth)
        }),
        new("W2 측면 위협", 60f, new List<StllWaveSpawnEntry>
        {
            new(StllEnemyKind.Grunt, 10, 5f, StllPrimitiveMapBuilder.SpawnNorth),
            new(StllEnemyKind.Archer, 6, 7f, StllPrimitiveMapBuilder.SpawnEast)
        }),
        new("W3 화공", 130f, new List<StllWaveSpawnEntry>
        {
            new(StllEnemyKind.Arsonist, 6, 8f, StllPrimitiveMapBuilder.SpawnNorth),
            new(StllEnemyKind.Arsonist, 4, 8f, StllPrimitiveMapBuilder.SpawnEast),
            new(StllEnemyKind.Grunt, 8, 6f, StllPrimitiveMapBuilder.SpawnWest)
        }),
        new("W4 C 거점", 210f, new List<StllWaveSpawnEntry>
        {
            new(StllEnemyKind.Charger, 4, 5f, StllPrimitiveMapBuilder.SpawnEast),
            new(StllEnemyKind.Charger, 4, 5f, StllPrimitiveMapBuilder.SpawnWest),
            new(StllEnemyKind.Archer, 8, 6f, StllPrimitiveMapBuilder.SpawnEast)
        }),
        new("W5 최후의 파도", 300f, new List<StllWaveSpawnEntry>
        {
            new(StllEnemyKind.Charger, 6, 5f, StllPrimitiveMapBuilder.SpawnNorth),
            new(StllEnemyKind.Arsonist, 4, 6f, StllPrimitiveMapBuilder.SpawnWest),
            new(StllEnemyKind.Grunt, 8, 5f, StllPrimitiveMapBuilder.SpawnEast)
        }),
        new("W6 호위병", 390f, new List<StllWaveSpawnEntry>
        {
            new(StllEnemyKind.EliteGuard, 4, 8f, StllPrimitiveMapBuilder.SpawnEast),
            new(StllEnemyKind.Archer, 8, 5f, StllPrimitiveMapBuilder.SpawnNorth),
            new(StllEnemyKind.Charger, 4, 6f, StllPrimitiveMapBuilder.SpawnWest)
        }),
        new("보스 화웅", 450f, new List<StllWaveSpawnEntry>
        {
            new(StllEnemyKind.Grunt, 0, 1f, StllPrimitiveMapBuilder.SpawnNorth, true)
        })
    };
}
