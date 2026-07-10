using UnityEngine;

/// <summary>Epic Toon FX / CFX 프리팹 기본 회전 보정.</summary>
public static class CwslEtfxVfxOrientation
{
    // ETFX Sword Wave — 돌진 파동이 하늘을 보지 않도록 X축 90° 보정 + 전후 정렬.
    public static readonly Quaternion ShieldDashWaveWorldRotationOffset = Quaternion.Euler(0f, 180f, 0f);
    public static readonly Quaternion ShieldDashWaveAttachLocalRotation = Quaternion.Euler(90f, 0f, 0f);

    // ETFX Sword Whirlwind — 수평 방패에 그대로 부착
    public static readonly Quaternion ShieldWhirlwindAttachRotation = Quaternion.identity;

    // ETFX StunnedCirclingStars — 루트 X -90°, 머리 위 수평 별
    public static readonly Quaternion HeadStatusAttachRotation = Quaternion.Euler(90f, 0f, 0f);

    // ETFX BodySlam / 지면 충격 — 루트 X -90° 보정
    public static readonly Quaternion GroundSlamRotationOffset = Quaternion.Euler(-90f, 0f, 0f);

    // ETFX SpinPortal — 풀 반환 시 identity로 리셋되므로 스폰 시 지면용 X -90° 재적용
    public static readonly Quaternion GroundZonePortalRotation = Quaternion.Euler(-90f, 0f, 0f);
}
