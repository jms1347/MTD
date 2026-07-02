using System.Collections.Generic;
using UnityEngine;

public class CoopSupplyCache : MonoBehaviour
{
    private const float TickInterval = 2.5f;
    private const int GoldPerTick = 8;

    private readonly Dictionary<string, float> nextTickByPlayer = new();
    private float pulse;

    private void Awake()
    {
        var trigger = gameObject.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = new Vector3(1.4f, 1.2f, 1.4f);
        trigger.center = new Vector3(0f, 0.6f, 0f);
    }

    private void Update()
    {
        pulse += Time.deltaTime * 2.5f;
        transform.localScale = Vector3.one * (1f + Mathf.Sin(pulse) * 0.03f);
    }

    private void OnTriggerStay(Collider other)
    {
        var unit = other.GetComponentInParent<CoopPlayerTowerUnit>();
        if (unit == null)
            return;

        var session = CoopGameSession.Instance;
        if (session == null || !session.IsHostAuthority)
            return;

        if (!session.TryGetPlayer(unit.PlayerId, out var player))
            return;

        if (!nextTickByPlayer.TryGetValue(player.playerId, out var nextTick))
            nextTick = 0f;

        if (Time.time < nextTick)
            return;

        nextTickByPlayer[player.playerId] = Time.time + TickInterval;
        player.gold += GoldPerTick;
    }
}
