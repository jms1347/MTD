using UnityEngine;

public struct MonsterSpawnContext
{
    public Vector3 spawnPosition;
    public float groundY;
    public float fallSpeed;
}

public interface IMonsterMobility
{
    bool IsLanded { get; }
    void Reset(Monster monster, in MonsterSpawnContext context);
    void Tick(Monster monster);
}
