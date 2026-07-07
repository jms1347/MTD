using UnityEngine;

/// <summary>Epic Toon FX / CFX 프리팹 기본 회전 보정.</summary>
public static class CwslEtfxVfxOrientation
{
    // ETFX Sword Wave — 루트 X -90°, 세로 방패 전방 부착 시 보정
    public static readonly Quaternion ShieldAttachRotation = Quaternion.Euler(90f, 0f, 0f);

    // ETFX Sword Whirlwind — 루트 X -90°, 수평 방패에 그대로 부착 (Wave 보정과 다름)
    public static readonly Quaternion ShieldWhirlwindAttachRotation = Quaternion.identity;
}
