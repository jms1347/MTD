using UnityEngine;

/// <summary>
/// Kawaii Slimes Rig_* 애니메이션의 AlertObservers 이벤트 수신.
/// EnemyAi 제거 후에도 Rig_Attack 등 이벤트 경고가 나지 않도록 처리합니다.
/// </summary>
public class CwslSlimeAnimationEventReceiver : MonoBehaviour
{
    public void AlertObservers(string message)
    {
        // CwSL 몬스터 AI는 CwslMeleeMonster 등에서 제어 — 데모용 상태 전환은 불필요.
    }
}
