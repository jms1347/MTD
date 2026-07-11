using Unity.Netcode;
using UnityEngine;

public class StllEnemyHealth : NetworkBehaviour
{
    private readonly NetworkVariable<float> health = new(
        80f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private Vector3 knockbackVelocity;
    private float knockbackDamping = 8f;

    public bool IsAlive => health.Value > 0f;

    public void ConfigureServer(float maxHealth)
    {
        if (!IsServer)
            return;

        health.Value = maxHealth;
    }

    public void TakeDamageServer(float amount, ulong attackerClientId, Vector3 knockback)
    {
        if (!IsServer || !IsAlive)
            return;

        health.Value = Mathf.Max(0f, health.Value - amount);
        knockbackVelocity += knockback;

        if (health.Value <= 0f)
            NetworkObject.Despawn(true);
    }

    public void ApplyKnockbackServer(Vector3 knockback, float damage, ulong attackerClientId)
    {
        TakeDamageServer(damage, attackerClientId, knockback);
    }

    private void Update()
    {
        if (!IsServer || knockbackVelocity.sqrMagnitude < 0.01f)
            return;

        var delta = knockbackVelocity * Time.deltaTime;
        transform.position += delta;
        knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, knockbackDamping * Time.deltaTime);
    }
}
