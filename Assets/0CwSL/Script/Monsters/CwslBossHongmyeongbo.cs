using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 홍명보 보스 — 업보 38억 도달 시 소환 (전투 로직은 추후 확장).
/// </summary>
public class CwslBossHongmyeongbo : CwslMonsterBase
{
    public override void Initialize(CwslMonsterType type)
    {
        base.Initialize(type);
        moveSpeed = 2.2f;
    }

    protected override void TickServerAI()
    {
        if (!IsValidTarget(currentTarget))
            return;

        MoveToward(currentTarget.transform.position, 0.75f);
    }
}
