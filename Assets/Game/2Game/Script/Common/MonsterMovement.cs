using UnityEngine;

/// <summary>
/// 몬스터 이동 목표(레인 → 넥서스)를 통합합니다.
/// </summary>
public static class MonsterMovement
{
    public static Vector3 ResolveMoveTarget(Monster monster, Transform nexusTarget)
    {
        if (monster == null)
            return nexusTarget != null ? nexusTarget.position : Vector3.zero;

        var laneFollower = monster.GetComponent<MonsterLaneFollower>();
        if (laneFollower != null && laneFollower.TryGetMoveTarget(out Vector3 laneTarget))
            return laneTarget;

        return nexusTarget != null ? nexusTarget.position : monster.transform.position;
    }

    public static void ConfigureLane(Monster monster, SpawnDirection direction, Vector3 spawnPosition)
    {
        if (monster == null)
            return;

        var laneFollower = monster.GetComponent<MonsterLaneFollower>();
        if (laneFollower == null)
            laneFollower = monster.gameObject.AddComponent<MonsterLaneFollower>();

        laneFollower.Configure(direction, spawnPosition);
        monster.Navigator?.ClearPath();
    }
}
