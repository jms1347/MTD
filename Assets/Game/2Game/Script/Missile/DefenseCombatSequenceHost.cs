using UnityEngine;

/// <summary>
/// 지연 유성·화산 분출 등 코루틴 시퀀스 실행용 호스트.
/// </summary>
public class DefenseCombatSequenceHost : MonoBehaviour
{
    private static DefenseCombatSequenceHost instance;

    public static DefenseCombatSequenceHost Ensure()
    {
        if (instance != null)
            return instance;

        var go = new GameObject(nameof(DefenseCombatSequenceHost));
        DontDestroyOnLoad(go);
        instance = go.AddComponent<DefenseCombatSequenceHost>();
        return instance;
    }
}
