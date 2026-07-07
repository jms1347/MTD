using Unity.Netcode;
using UnityEngine;

/// <summary>데미지 팝업 — 서버 ClientRpc + 로컬 즉시 표시(호스트/폴백 보장).</summary>
public static class CwslDamageFeedback
{
    public static void Play(Vector3 worldAnchor, float damage, CwslDamagePopupKind kind)
    {
        CwslDamagePopupPool.EnsureReady();
        CwslDamagePopupPool.Play(worldAnchor, damage, kind);
    }

    public static void PlayFromServer(Vector3 worldAnchor, float damage, CwslDamagePopupKind kind)
    {
        var network = NetworkManager.Singleton;
        if (network == null || !network.IsServer)
        {
            Play(worldAnchor, damage, kind);
            return;
        }

        var session = CwslGameSession.Instance;
        if (session != null && session.IsSpawned)
        {
            session.ReportDamagePopupClientRpc(worldAnchor, damage, (int)kind);
            return;
        }

        Play(worldAnchor, damage, kind);
    }
}
