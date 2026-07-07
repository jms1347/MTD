using System.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 빨간 마법??Q ??지�?지??메테?? ?�하 ??광역 ?�해 + 그을�??�펙??
/// </summary>
public class CwslRedMageMeteorSkill : CwslPlayerSkillBase
{
    private const float MeteorRadius = 4.8f;
    private const float FallHeight = 18f;
    private const float FallDuration = 0.55f;
    private const float BurnLifetime = 3.2f;

    private CwslPlayerSkillCooldowns skillCooldowns;

    public override CwslSkillActivationType ActivationType => CwslSkillActivationType.GroundTarget;

    public override bool IsActiveForCharacter(CwslCharacterId characterId) =>
        characterId == CwslCharacterId.RedMage;

    public override void OnNetworkSpawn()
    {
        skillCooldowns = GetComponent<CwslPlayerSkillCooldowns>();
    }

    public override bool CanCastServer(ulong senderClientId)
    {
        if (!IsServer)
            return false;

        if (skillCooldowns != null && !skillCooldowns.IsReady(0))
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

        if (skillCooldowns != null && !skillCooldowns.IsReady(0))
            return;

        skillCooldowns?.BeginCooldown(0);
        var impactPoint = worldPoint;
        impactPoint.y = 0f;

        // VFX??PlayerSkills ClientRpc?�서 ?�역 반경 ?�함???�생
        GetComponent<CwslPlayerSkills>()?.PlayMeteorFxClientRpc(impactPoint);
        StartCoroutine(ApplyDamageAfterFall(impactPoint));
    }

    private IEnumerator ApplyDamageAfterFall(Vector3 impactPoint)
    {
        yield return new WaitForSeconds(FallDuration);

        var meteorDamage = GetComponent<CwslPlayerCharacter>() != null
            ? CwslCharacterStatCatalog.GetAttackPower(GetComponent<CwslPlayerCharacter>().CharacterId)
            : CwslGameConstants.AttackDamage;

        var monsters = CwslCombatRegistry.AliveMonsters;
        var radiusSqr = MeteorRadius * MeteorRadius;
        foreach (var monster in monsters)
        {
            if (monster == null || !monster.IsAlive)
                continue;

            var flat = monster.GetFlatHitPoint() - impactPoint;
            flat.y = 0f;
            if (flat.sqrMagnitude > radiusSqr)
                continue;

            monster.DamageFromPlayer(OwnerClientId, meteorDamage);
        }

        if (NetworkManager.Singleton == null)
            yield break;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var playerObject = client.PlayerObject;
            if (playerObject == null || playerObject.OwnerClientId == OwnerClientId)
                continue;

            var playerHealth = playerObject.GetComponent<CwslPlayerHealth>();
            if (playerHealth == null || !playerHealth.IsAlive)
                continue;

            var flat = playerObject.transform.position - impactPoint;
            flat.y = 0f;
            if (flat.sqrMagnitude > radiusSqr)
                continue;

            playerHealth.TryReceiveExplosionHitServer(meteorDamage, playerObject.transform.position);
        }
    }
}
