using UnityEngine;

[RequireComponent(typeof(Health))]
public class PooledEnemy : MonoBehaviour
{
    private Health health;
    private bool isScheduledForReturn;

    public string PoolCode { get; set; }

    private void Awake()
    {
        health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        isScheduledForReturn = false;

        if (health != null)
            health.OnDeath += HandleDeath;
    }

    private void OnDisable()
    {
        if (health != null)
            health.OnDeath -= HandleDeath;

        isScheduledForReturn = false;
    }

    private void HandleDeath()
    {
        if (isScheduledForReturn)
            return;

        DefenseKillGoldRewards.TryGrant(transform.position);
        RoguelikeRunEvents.NotifyEnemyKilled();
        isScheduledForReturn = true;
        StageManager.Instance?.ScheduleReturnEnemy(gameObject);
    }
}
