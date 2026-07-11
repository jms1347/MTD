using Unity.Netcode;
using UnityEngine;

/// <summary>장수의 위엄 — 8m 오라, 부하 공격 +15%.</summary>
public class StllCommanderAura : NetworkBehaviour
{
    public float GetMinionDamageMultiplier(Vector3 minionPosition)
    {
        var flat = minionPosition - transform.position;
        flat.y = 0f;
        if (flat.magnitude > StllGlaiveConstants.CommanderAuraRadius)
            return 1f;

        return 1f + StllGlaiveConstants.CommanderAuraAttackBonus;
    }
}
