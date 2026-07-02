using UnityEngine;

/// <summary>
/// 슬라임 애니메이션 클립 이벤트 → 루트 MonsterSlimeVisual 전달.
/// </summary>
public class MonsterSlimeAnimationRelay : MonoBehaviour
{
    public void AlertObservers(string message)
    {
        var visual = GetComponentInParent<MonsterSlimeVisual>();
        visual?.OnAnimationEvent(message);
    }
}
