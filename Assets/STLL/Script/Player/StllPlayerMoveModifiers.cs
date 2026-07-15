using Unity.Netcode;
using UnityEngine;

/// <summary>이동·공격 배율 통합.</summary>
public class StllPlayerMoveModifiers : NetworkBehaviour
{
    private StllBrotherhoodRoleState roleState;
    private StllPlayerCardInventory cards;
    private StllPlayerLoadout loadout;

    private void Awake()
    {
        roleState = GetComponent<StllBrotherhoodRoleState>();
        cards = GetComponent<StllPlayerCardInventory>();
        loadout = GetComponent<StllPlayerLoadout>();
    }

    public float GetSpeedMultiplier()
    {
        var mult = 1f;
        if (roleState != null)
            mult *= StllRoleCombatModifiers.GetMoveSpeedMultiplier(roleState.Role);
        if (cards != null)
            mult *= 1f + cards.GetPassiveBonus(StllPassiveBonusType.MoveSpeed);
        if (loadout != null)
            mult *= loadout.GetHorseSpeedMultiplier();
        mult *= 1f + GetLiuBeiAuraBonus();
        return mult;
    }

    public float GetDamageMultiplier()
    {
        var mult = 1f;
        if (roleState != null)
            mult *= StllRoleCombatModifiers.GetAttackDamageMultiplier(roleState.Role);
        if (cards != null)
            mult *= 1f + cards.GetPassiveBonus(StllPassiveBonusType.AttackDamage);
        if (loadout != null)
            mult *= loadout.GetWeaponDamageMultiplier();
        if (StllTeamGold.Instance != null)
            mult *= 1f + StllTeamGold.Instance.GetTeamAttackBonus();
        return mult;
    }

    private float GetLiuBeiAuraBonus()
    {
        var liuBei = FindObjectsByType<StllBrotherhoodRoleState>(FindObjectsSortMode.None);
        for (var i = 0; i < liuBei.Length; i++)
        {
            if (liuBei[i].Role != StllBrotherhoodRole.LiuBei)
                continue;

            var flat = transform.position - liuBei[i].transform.position;
            flat.y = 0f;
            if (flat.magnitude <= 8f)
                return StllRoleCombatModifiers.GetAllyMoveSpeedAuraBonus(StllBrotherhoodRole.LiuBei);
        }

        return 0f;
    }
}
