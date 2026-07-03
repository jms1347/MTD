using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 미사일 탱크 전투.
/// A — 미사일 1발 (선택/가까운 적, 없으면 포신 방향) / 골드 1
/// Q — 최대 12발 부채꼴 관통 (포신 방향, 타겟 없어도 발사) / 발 수만큼 골드
/// </summary>
public class CwslMissileTankSkill : CwslPlayerSkillBase
{
    private const float MissileRange = 24f;
    private const float FireCooldown = 0.45f;
    private const float FanCooldown = 0.7f;
    private const float MissileSpeed = 18f;
    private const float MissileLifetime = 7f;
    private const float MissileDamage = 1f;
    private const int MaxFanMissiles = 12;
    private const float FanAngleDegrees = 56f;

    private float nextFireTime;
    private CwslPlayerCharacter playerCharacter;
    private CwslPlayerSelection selection;
    private CwslPlayerCannonAim cannonAim;
    private CwslPlayerGold playerGold;

    public override CwslSkillActivationType ActivationType => CwslSkillActivationType.Instant;

    public override bool IsActiveForCharacter(CwslCharacterId characterId) =>
        characterId == CwslCharacterId.MissileTank;

    // Q 발사는 TryFireAttackServer(true)로 직접 처리
    public override bool CanCastServer(ulong senderClientId) => false;

    public override void OnNetworkSpawn()
    {
        playerCharacter = GetComponent<CwslPlayerCharacter>();
        selection = GetComponent<CwslPlayerSelection>();
        cannonAim = GetComponent<CwslPlayerCannonAim>();
        playerGold = GetComponent<CwslPlayerGold>();
    }

    private void Update()
    {
        if (!IsServer || playerCharacter == null || playerCharacter.CharacterId != CwslCharacterId.MissileTank)
            return;

        // 적이 있으면 포신이 따라봄 (없어도 발사 가능)
        if (TryResolveTarget(out var target))
            cannonAim?.SetAimServer(target.transform.position + Vector3.up * 1.1f);
    }

    /// <param name="fanMode">false=A 단발, true=Q 멀티샷</param>
    public bool TryFireAttackServer(bool fanMode)
    {
        if (!IsServer || playerCharacter == null || playerCharacter.CharacterId != CwslCharacterId.MissileTank)
            return false;

        if (Time.time < nextFireTime)
            return false;

        if (playerGold == null || playerGold.Gold < 1)
            return false;

        var centerDirection = ResolveFireDirection(out var aimPoint);
        cannonAim?.SetAimServer(aimPoint);

        if (fanMode)
        {
            var missileCount = Mathf.Min(MaxFanMissiles, playerGold.Gold);
            if (missileCount < 1)
                return false;

            if (!playerGold.TrySpendGoldServer(missileCount))
                return false;

            nextFireTime = Time.time + FanCooldown;
            FireFanServer(centerDirection, missileCount);
            PlayFireClientRpc();
            return true;
        }

        if (!playerGold.TrySpendGoldServer(CwslGameConstants.SkillGoldCost))
            return false;

        nextFireTime = Time.time + FireCooldown;
        FireProjectileServer(centerDirection, piercing: false);
        PlayFireClientRpc();
        return true;
    }

    private Vector3 ResolveFireDirection(out Vector3 aimPoint)
    {
        var muzzle = cannonAim != null
            ? cannonAim.GetMuzzlePosition()
            : transform.position + Vector3.up * 1.2f + transform.forward * 0.9f;

        // 선택/가까운 적이 있으면 그쪽, 없으면 현재 포신 방향
        if (TryResolveTarget(out var target))
        {
            aimPoint = target.transform.position + Vector3.up * 1.1f;
            var toTarget = aimPoint - muzzle;
            if (toTarget.sqrMagnitude > 0.0001f)
                return toTarget.normalized;
        }

        var forward = cannonAim != null ? cannonAim.GetMuzzleForward() : transform.forward;
        if (forward.sqrMagnitude < 0.0001f)
            forward = transform.forward;

        forward.Normalize();
        aimPoint = muzzle + forward * 10f;
        return forward;
    }

    private bool TryResolveTarget(out CwslMonsterHealth target)
    {
        target = null;

        if (selection != null &&
            selection.TryGetSelectedTarget(out var selected) &&
            selected != null)
        {
            var selectedHealth = selected.GetComponent<CwslMonsterHealth>();
            if (selectedHealth != null &&
                selectedHealth.IsAlive &&
                IsInRange(selected.transform.position))
            {
                target = selectedHealth;
                return true;
            }
        }

        return TryGetNearestMonster(out target);
    }

    private bool TryGetNearestMonster(out CwslMonsterHealth target)
    {
        target = null;
        var bestDistance = float.MaxValue;
        var monsters = FindObjectsByType<CwslMonsterHealth>(FindObjectsSortMode.None);
        foreach (var monster in monsters)
        {
            if (monster == null || !monster.IsAlive)
                continue;

            var flat = monster.transform.position - transform.position;
            flat.y = 0f;
            var distance = flat.magnitude;
            if (distance > MissileRange || distance >= bestDistance)
                continue;

            bestDistance = distance;
            target = monster;
        }

        return target != null;
    }

    private bool IsInRange(Vector3 worldPosition)
    {
        var flat = worldPosition - transform.position;
        flat.y = 0f;
        return flat.magnitude <= MissileRange;
    }

    private void FireFanServer(Vector3 centerDirection, int missileCount)
    {
        if (missileCount == 1)
        {
            FireProjectileServer(centerDirection, piercing: true);
            return;
        }

        var halfSpread = FanAngleDegrees * 0.5f;
        for (var i = 0; i < missileCount; i++)
        {
            var t = i / (float)(missileCount - 1);
            var yaw = Mathf.Lerp(-halfSpread, halfSpread, t);
            var direction = Quaternion.AngleAxis(yaw, Vector3.up) * centerDirection;
            direction.y = centerDirection.y;
            if (direction.sqrMagnitude > 0.0001f)
                direction.Normalize();
            FireProjectileServer(direction, piercing: true);
        }
    }

    private void FireProjectileServer(Vector3 fireDirection, bool piercing)
    {
        var session = CwslGameSession.Instance;
        if (session == null || session.Assets.playerMissilePrefab == null)
            return;

        var muzzle = cannonAim != null
            ? cannonAim.GetMuzzlePosition()
            : transform.position + Vector3.up * 1.2f + transform.forward * 0.9f;

        if (fireDirection.sqrMagnitude < 0.0001f)
            fireDirection = transform.forward;

        var networkObject = CwslNetworkPoolService.Instance?.Get(
            session.Assets.playerMissilePrefab,
            muzzle,
            Quaternion.LookRotation(fireDirection, Vector3.up));
        if (networkObject == null)
            return;

        var projectile = networkObject.GetComponent<CwslPlayerProjectile>();
        projectile?.Configure(fireDirection, MissileSpeed, MissileLifetime, OwnerClientId, MissileDamage, piercing);
    }

    [ClientRpc]
    private void PlayFireClientRpc()
    {
        var visual = transform.Find("Visual");
        visual?.GetComponent<CwslPlayerCannonRecoilVisual>()?.PlayFire();
    }
}
