using Unity.Netcode;
using UnityEngine;

/// <summary>F — 부하 수행 ↔ 사수 모드 토글.</summary>
public class StllMinionCommander : NetworkBehaviour
{
    private readonly NetworkVariable<byte> commandMode = new(
        (byte)StllMinionCommandMode.Follow,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly System.Collections.Generic.List<NetworkObject> minions = new();

    public StllMinionCommandMode Mode => (StllMinionCommandMode)commandMode.Value;

    public void RegisterMinionServer(NetworkObject minion)
    {
        if (!IsServer || minion == null || minions.Contains(minion))
            return;

        minions.Add(minion);
        var ai = minion.GetComponent<StllMinionAI>();
        ai?.AssignCommanderServer(NetworkObject, Mode, minion.transform.position);
    }

    public void ToggleModeServer()
    {
        if (!IsServer)
            return;

        var next = Mode == StllMinionCommandMode.Follow
            ? StllMinionCommandMode.Hold
            : StllMinionCommandMode.Follow;
        commandMode.Value = (byte)next;

        var holdPos = transform.position - transform.forward * 2f;
        for (var i = 0; i < minions.Count; i++)
        {
            var minion = minions[i];
            if (minion == null)
                continue;

            minion.GetComponent<StllMinionAI>()?.SetModeServer(next, holdPos);
        }
    }
}
