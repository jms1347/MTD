using UnityEngine;

public class GroundMonster : MonoBehaviour, IMonsterMobility
{
    public const float HoverOffset = 0f;

    public bool IsLanded { get; private set; } = true;

    public void Reset(Monster monster, in MonsterSpawnContext context)
    {
        IsLanded = true;
        var position = context.spawnPosition;
        position.y = context.groundY;
        monster.transform.position = position;
    }

    public void Tick(Monster monster)
    {
    }
}
