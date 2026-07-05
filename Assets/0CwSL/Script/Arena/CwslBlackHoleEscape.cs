using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

/// <summary>블랙홀 존 — 중심 반대 방향 광클(이동 명령)로 밀려 나갈 수 있습니다.</summary>
public class CwslBlackHoleEscape : NetworkBehaviour
{
    private float lastEscapeClickTime;

    public void TryRegisterMoveAwayClickServer(Vector3 destination)
    {
        if (!IsServer)
            return;

        var position = transform.position;
        if (!CwslArenaZones.IsInBlackHoleZone(position))
            return;

        if (Time.time - lastEscapeClickTime < CwslGameConstants.BlackHoleEscapeClickCooldown)
            return;

        var center = CwslArenaZones.GetBlackHoleCenter();
        var toClick = destination - position;
        toClick.y = 0f;
        if (toClick.sqrMagnitude < 0.08f)
            return;

        var fromCenter = position - center;
        fromCenter.y = 0f;

        if (fromCenter.sqrMagnitude > 0.12f)
        {
            var awayDot = Vector3.Dot(toClick.normalized, fromCenter.normalized);
            if (awayDot < CwslGameConstants.BlackHoleEscapeAwayDotThreshold)
                return;
        }

        lastEscapeClickTime = Time.time;
        ApplyEscapePushServer(center, position, toClick.normalized);
    }

    private void ApplyEscapePushServer(Vector3 center, Vector3 position, Vector3 clickDirection)
    {
        var away = position - center;
        away.y = 0f;
        if (away.sqrMagnitude < 0.12f)
            away = clickDirection;

        if (away.sqrMagnitude < 0.01f)
            return;

        away.Normalize();
        var next = position + away * CwslGameConstants.BlackHoleEscapeClickPush;
        next.y = position.y;

        var rammer = GetComponent<CwslMomentumRammerSkill>();
        if (rammer != null && rammer.IsMomentumActive)
        {
            transform.position = next;
            return;
        }

        var agent = GetComponent<NavMeshAgent>();
        if (agent != null && agent.enabled && agent.isOnNavMesh)
            agent.Warp(next);
        else
            transform.position = next;
    }
}
