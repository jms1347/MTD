using UnityEngine;

/// <summary>몬스터 1회 피격 무시 보호막 (수석 코치 철밥통 쉴드).</summary>
public class CwslMonsterHitShield : MonoBehaviour
{
    private int hitsRemaining;

    public bool HasShield => hitsRemaining > 0;

    public static CwslMonsterHitShield Ensure(GameObject root)
    {
        if (root == null)
            return null;

        var shield = root.GetComponent<CwslMonsterHitShield>();
        if (shield == null)
            shield = root.AddComponent<CwslMonsterHitShield>();

        return shield;
    }

    public void GrantServer(int hits)
    {
        hitsRemaining = Mathf.Max(0, hits);
        if (hitsRemaining > 0)
            GetComponent<CwslMonsterHealth>()?.NotifyHitShieldGrantedServer();
    }

    public void ClearServer()
    {
        if (hitsRemaining <= 0)
            return;

        hitsRemaining = 0;
        GetComponent<CwslMonsterHealth>()?.NotifyHitShieldBrokenServer();
    }

    public bool TryConsumeHitServer()
    {
        if (hitsRemaining <= 0)
            return false;

        hitsRemaining--;
        var health = GetComponent<CwslMonsterHealth>();
        if (hitsRemaining <= 0)
            health?.NotifyHitShieldBrokenServer();
        else
            health?.NotifyHitShieldBlockedServer();

        return true;
    }
}
