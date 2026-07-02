using System.Collections.Generic;
using UnityEngine;

public class CoopHealZone : MonoBehaviour
{
    private const float TickInterval = 2f;
    private const float HealPerTick = 6f;

    private readonly Dictionary<string, float> nextTickByPlayer = new();

    private void Awake()
    {
        var trigger = gameObject.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = new Vector3(2.4f, 1.4f, 2.4f);
        trigger.center = new Vector3(0f, 0.7f, 0f);
    }

    private void OnTriggerStay(Collider other)
    {
        var unit = other.GetComponentInParent<CoopPlayerTowerUnit>();
        if (unit == null)
            return;

        var session = CoopGameSession.Instance;
        if (session == null || !session.IsHostAuthority || !session.TryGetPlayer(unit.PlayerId, out var player))
            return;

        if (player.towerHp <= 0f || player.towerHp >= player.towerMaxHp)
            return;

        if (!nextTickByPlayer.TryGetValue(player.playerId, out var nextTick))
            nextTick = 0f;
        if (Time.time < nextTick)
            return;

        nextTickByPlayer[player.playerId] = Time.time + TickInterval;
        player.towerHp = Mathf.Min(player.towerMaxHp, player.towerHp + HealPerTick);

        if (session.TryGetLivingTower(unit.PlayerId, out var tower) && tower != null)
        {
            var health = tower.GetComponent<Health>();
            health?.Heal(HealPerTick);
        }
    }
}
