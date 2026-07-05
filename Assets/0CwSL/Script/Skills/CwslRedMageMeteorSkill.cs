using System.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 빨간 마법사 Q — 지면 지정 메테오. 낙하 후 광역 피해 + 그을림 이펙트.
/// </summary>
public class CwslRedMageMeteorSkill : CwslPlayerSkillBase
{
    private const float MeteorDamage = 1f;
    private const float MeteorRadius = 4.8f;
    private const float FallHeight = 18f;
    private const float FallDuration = 0.55f;
    private const float BurnLifetime = 3.2f;
    private const float Cooldown = 0.85f;

    private float nextCastTime;

    public override CwslSkillActivationType ActivationType => CwslSkillActivationType.GroundTarget;

    public override bool IsActiveForCharacter(CwslCharacterId characterId) =>
        characterId == CwslCharacterId.RedMage;

    public override bool CanCastServer(ulong senderClientId)
    {
        if (!IsServer)
            return false;

        if (Time.time < nextCastTime)
            return false;

        var gold = GetComponent<CwslPlayerGold>();
        if (!CwslGameConstants.SkillsConsumeGold)
            return gold != null;

        return gold != null && gold.Gold >= CwslGameConstants.MeteorGoldCost;
    }

    public override void OnSkillGroundTargetServer(ulong senderClientId, Vector3 worldPoint)
    {
        if (!IsServer)
            return;

        if (Time.time < nextCastTime)
            return;

        nextCastTime = Time.time + Cooldown;
        var impactPoint = worldPoint;
        impactPoint.y = 0f;

        // VFX는 PlayerSkills ClientRpc에서 영역 반경 포함해 재생
        GetComponent<CwslPlayerSkills>()?.PlayMeteorFxClientRpc(impactPoint);
        StartCoroutine(ApplyDamageAfterFall(impactPoint));
    }

    private IEnumerator ApplyDamageAfterFall(Vector3 impactPoint)
    {
        yield return new WaitForSeconds(FallDuration);

        var monsters = FindObjectsByType<CwslMonsterHealth>(FindObjectsSortMode.None);
        var radiusSqr = MeteorRadius * MeteorRadius;
        foreach (var monster in monsters)
        {
            if (monster == null || !monster.IsAlive)
                continue;

            var flat = monster.GetFlatHitPoint() - impactPoint;
            flat.y = 0f;
            if (flat.sqrMagnitude > radiusSqr)
                continue;

            monster.DamageFromPlayer(OwnerClientId, MeteorDamage);
        }
    }
}
