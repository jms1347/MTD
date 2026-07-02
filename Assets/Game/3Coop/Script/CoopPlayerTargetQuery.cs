using UnityEngine;

public static class CoopPlayerTargetQuery
{
    public static Transform FindNearestLivingTower(Vector3 fromPosition)
    {
        if (CoopGameSession.Instance == null)
            return null;

        CoopPlayerTowerUnit bestUnit = null;
        var bestDist = float.MaxValue;

        foreach (var unit in Object.FindObjectsByType<CoopPlayerTowerUnit>(FindObjectsSortMode.None))
        {
            if (unit == null)
                continue;

            var health = unit.GetComponent<Health>();
            if (health != null && !health.IsAlive)
                continue;

            var dist = Vector3.Distance(fromPosition, unit.transform.position);
            if (dist >= bestDist)
                continue;

            bestDist = dist;
            bestUnit = unit;
        }

        return bestUnit != null ? bestUnit.transform : null;
    }
}
