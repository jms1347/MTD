using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>플레이어 표시 이름 — 소유 클라이언트가 서버에 등록합니다.</summary>
public class CwslPlayerProfile : NetworkBehaviour
{
    private const int MaxNameLength = 24;

    private readonly NetworkVariable<FixedString32Bytes> displayName = new(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public string DisplayName
    {
        get
        {
            var value = displayName.Value.ToString();
            return string.IsNullOrWhiteSpace(value) ? $"Player {OwnerClientId}" : value;
        }
    }

    public static event System.Action OnAnyProfileChanged;

    public event System.Action<string> OnDisplayNameChanged;

    public override void OnNetworkSpawn()
    {
        displayName.OnValueChanged += HandleDisplayNameChanged;
        NotifyDisplayNameChanged();

        if (IsOwner)
            SubmitDisplayNameServerRpc(ResolveLocalDisplayName());
        else if (IsServer && string.IsNullOrWhiteSpace(displayName.Value.ToString()))
            displayName.Value = new FixedString32Bytes($"Player {OwnerClientId}");
    }

    public override void OnNetworkDespawn()
    {
        displayName.OnValueChanged -= HandleDisplayNameChanged;
    }

    private static string ResolveLocalDisplayName()
    {
        var lobby = LobbyNetworkManager.Instance;
        if (lobby != null && !string.IsNullOrWhiteSpace(lobby.LocalPlayerName))
            return lobby.LocalPlayerName.Trim();

        if (NetworkManager.Singleton != null)
            return $"Player {NetworkManager.Singleton.LocalClientId}";

        return "Player";
    }

    [ServerRpc]
    private void SubmitDisplayNameServerRpc(string name)
    {
        displayName.Value = SanitizeName(name);
    }

    private static FixedString32Bytes SanitizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return default;

        var trimmed = name.Trim();
        if (trimmed.Length > MaxNameLength)
            trimmed = trimmed.Substring(0, MaxNameLength);

        return new FixedString32Bytes(trimmed);
    }

    private void HandleDisplayNameChanged(FixedString32Bytes previous, FixedString32Bytes current)
    {
        NotifyDisplayNameChanged();
    }

    private void NotifyDisplayNameChanged()
    {
        var name = DisplayName;
        OnDisplayNameChanged?.Invoke(name);
        OnAnyProfileChanged?.Invoke();
    }
}
