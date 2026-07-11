using Unity.Netcode;
using UnityEngine;

/// <summary>Space — 마상 질주 (넉백).</summary>
public class StllMountedCharge : NetworkBehaviour
{
    private StllHorseMotor motor;
    private StllPlayerStamina stamina;
    private readonly System.Collections.Generic.HashSet<ulong> knockedThisCharge = new();

    private void Awake()
    {
        motor = GetComponent<StllHorseMotor>();
        stamina = GetComponent<StllPlayerStamina>();
    }

    public bool TryStartChargeServer()
    {
        if (!IsServer || motor == null || motor.IsCharging)
            return false;

        if (stamina != null && !stamina.TrySpendServer(StllGlaiveConstants.ChargeStaminaCost))
            return false;

        motor.BeginChargeServer();
        knockedThisCharge.Clear();
        return true;
    }

    public void OnChargeStartedServer()
    {
        knockedThisCharge.Clear();
    }

    public void TickChargeKnockbackServer(Vector3 direction, float speed)
    {
        if (!IsServer || speed < StllGlaiveConstants.ChargeSpeed * 0.5f)
            return;

        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
            return;
        direction.Normalize();

        var origin = transform.position + direction * 0.8f;
        var hits = Physics.OverlapSphere(origin, StllGlaiveConstants.ChargeKnockbackRadius);
        for (var i = 0; i < hits.Length; i++)
        {
            var health = hits[i].GetComponentInParent<StllEnemyHealth>();
            if (health == null || !health.IsAlive)
                continue;

            var id = health.NetworkObjectId;
            if (knockedThisCharge.Contains(id))
                continue;

            knockedThisCharge.Add(id);
            var knockDir = (health.transform.position - transform.position);
            knockDir.y = 0f;
            if (knockDir.sqrMagnitude < 0.01f)
                knockDir = direction;
            knockDir.Normalize();

            health.ApplyKnockbackServer(
                knockDir * StllGlaiveConstants.ChargeKnockbackDistance,
                StllGlaiveConstants.BasicAttackDamage * 0.6f,
                OwnerClientId);
        }
    }
}
