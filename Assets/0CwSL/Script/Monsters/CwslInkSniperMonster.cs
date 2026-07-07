using UnityEngine;

/// <summary>먹물 스나이퍼 — 가장 가까운 적, 명중 시 3초 시야 0.</summary>
public class CwslInkSniperMonster : CwslRangedMonster
{
    protected override float GetFireCooldown() => CwslGameConstants.InkSniperFireCooldownSeconds;

    protected override CwslMonsterProjectileKind GetProjectileKind() => CwslMonsterProjectileKind.InkBolt;

    protected override void PlayFireFx(Vector3 muzzlePosition, Vector3 fireDirection)
    {
        var rotation = fireDirection.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(fireDirection.normalized, Vector3.up)
            : transform.rotation;
        CwslVfxSpawner.SpawnShadowMuzzleFlash(muzzlePosition, rotation);
    }
}
