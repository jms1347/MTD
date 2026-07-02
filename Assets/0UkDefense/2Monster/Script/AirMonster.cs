using UnityEngine;

public class AirMonster : MonoBehaviour, IMonsterMobility
{
    public bool IsLanded { get; private set; }

    private float hoverY;
    private float fallSpeed;

    public void Reset(Monster monster, in MonsterSpawnContext context)
    {
        IsLanded = false;
        hoverY = context.groundY;
        fallSpeed = context.fallSpeed;
        monster.transform.position = context.spawnPosition;
    }

    public void Tick(Monster monster)
    {
        var position = monster.transform.position;
        position.y -= fallSpeed * Time.deltaTime;

        if (position.y <= hoverY)
        {
            position.y = hoverY;
            IsLanded = true;
        }

        monster.transform.position = position;
    }
}
