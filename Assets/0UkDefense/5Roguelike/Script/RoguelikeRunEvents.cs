using System;
using System.Collections.Generic;
using UnityEngine;

public static class RoguelikeRunEvents
{
    public static event Action OnEnemyKilled;
    public static event Action<long> OnGoldSpent;
    public static event Action OnTowerBuilt;

    public static void NotifyEnemyKilled() => OnEnemyKilled?.Invoke();
    public static void NotifyGoldSpent(long amount) => OnGoldSpent?.Invoke(amount);
    public static void NotifyTowerBuilt() => OnTowerBuilt?.Invoke();
}
