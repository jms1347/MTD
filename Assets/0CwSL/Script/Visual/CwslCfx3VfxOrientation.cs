using UnityEngine;

/// <summary>CFX3 프리팹 회전 보정 — 풀 Acquire 시 루트 회전이 identity로 리셋되므로 지면형은 복원.</summary>
public static class CwslCfx3VfxOrientation
{
    public static readonly Quaternion GroundHitLocalRotation = Quaternion.Euler(-90f, 0f, 0f);
}
