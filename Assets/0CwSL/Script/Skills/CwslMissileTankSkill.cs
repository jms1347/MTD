using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 미사일 탱크 Q — 선택 대상(또는 전방)으로 로켓 발사, 미사일 1발당 골드 1.
/// </summary>
public class CwslMissileTankSkill : CwslPlayerSkillBase
{
    private const float MissileRange = 24f;
    private const float FireCooldown = 0.5f;
    private const float MissileSpeed = 17f;
    private const float MissileLifetime = 7f;
    private const float MissileDamage = 1f;

    private float nextFireTime;
    private CwslPlayerCharacter playerCharacter;
    private CwslPlayerSelection selection;
    private CwslPlayerCannonAim cannonAim;

    public override CwslSkillActivationType ActivationType => CwslSkillActivationType.Instant;

    public override bool IsActiveForCharacter(CwslCharacterId characterId) =>
        characterId == CwslCharacterId.MissileTank;

    public override void OnNetworkSpawn()
    {
        playerCharacter = GetComponent<CwslPlayerCharacter>();
        selection = GetComponent<CwslPlayerSelection>();
        cannonAim = GetComponent<CwslPlayerCannonAim>();
    }

    private void Update()
    {
        if (!IsServer || playerCharacter == null || playerCharacter.CharacterId != CwslCharacterId.MissileTank)
            return;

        if (selection != null &&
            selection.TryGetSelectedTarget(out var target) &&
            target != null)
        {
            cannonAim?.SetAimServer(target.transform.position + Vector3.up * 1.1f);
        }
    }

    public override bool CanCastServer(ulong senderClientId)
    {
        if (!IsServer)
            return false;

        if (Time.time < nextFireTime)
            return false;

        var gold = GetComponent<CwslPlayerGold>();
        return gold != null && gold.Gold >= CwslGameConstants.SkillGoldCost;
    }

    public override void OnSkillPressedServer(ulong senderClientId)
    {
        if (!IsServer || playerCharacter == null || playerCharacter.CharacterId != CwslCharacterId.MissileTank)
            return;

        if (Time.time < nextFireTime)
            return;

        if (!TryResolveFireDirection(out var aimPoint, out var fireDirection))
            return;

        cannonAim?.SetAimServer(aimPoint);
        nextFireTime = Time.time + FireCooldown;
        FireProjectileServer(fireDirection);
        PlayFireClientRpc();
    }

    private bool TryResolveFireDirection(out Vector3 aimPoint, out Vector3 fireDirection)
    {
        aimPoint = transform.position + transform.forward * 8f + Vector3.up * 1f;
        fireDirection = transform.forward;

        if (selection != null &&
            selection.TryGetSelectedTarget(out var target) &&
            target != null)
        {
            var monsterHealth = target.GetComponent<CwslMonsterHealth>();
            if (monsterHealth != null && monsterHealth.IsAlive)
            {
                aimPoint = target.transform.position + Vector3.up * 1.1f;
                var flat = aimPoint - transform.position;
                flat.y = 0f;
                if (flat.magnitude <= MissileRange)
                {
                    fireDirection = (aimPoint - (cannonAim != null ? cannonAim.GetMuzzlePosition() : transform.position)).normalized;
                    return fireDirection.sqrMagnitude > 0.0001f;
                }
            }
        }

        aimPoint = transform.position + transform.forward * 10f + Vector3.up * 1f;
        fireDirection = transform.forward;
        return true;
    }

    private void FireProjectileServer(Vector3 fireDirection)
    {
        var session = CwslGameSession.Instance;
        if (session == null || session.Assets.playerMissilePrefab == null)
            return;

        var muzzle = cannonAim != null
            ? cannonAim.GetMuzzlePosition()
            : transform.position + Vector3.up * 1.2f + transform.forward * 0.9f;

        var networkObject = CwslNetworkPoolService.Instance?.Get(
            session.Assets.playerMissilePrefab,
            muzzle,
            Quaternion.LookRotation(fireDirection, Vector3.up));
        if (networkObject == null)
            return;

        var projectile = networkObject.GetComponent<CwslPlayerProjectile>();
        projectile?.Configure(fireDirection, MissileSpeed, MissileLifetime, OwnerClientId, MissileDamage);
    }

    [ClientRpc]
    private void PlayFireClientRpc()
    {
        var visual = transform.Find("Visual");
        visual?.GetComponent<CwslPlayerCannonRecoilVisual>()?.PlayFire();
    }
}
