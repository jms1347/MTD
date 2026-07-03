using Unity.Netcode;
using UnityEngine;

public class CwslPlayerInput : NetworkBehaviour
{
    private Camera playerCamera;
    private CwslPlayerMovement movement;
    private CwslPlayerSelection selection;
    private CwslPlayerSkills skills;
    private CwslPlayerGoldGift goldGift;
    private CwslPlayerCombat combat;
    private CwslPlayerHealth playerHealth;

    private bool skillHeld;

    public override void OnNetworkSpawn()
    {
        movement = GetComponent<CwslPlayerMovement>();
        selection = GetComponent<CwslPlayerSelection>();
        skills = GetComponent<CwslPlayerSkills>();
        goldGift = GetComponent<CwslPlayerGoldGift>();
        combat = GetComponent<CwslPlayerCombat>();
        playerHealth = GetComponent<CwslPlayerHealth>();

        if (IsOwner)
            playerCamera = Camera.main;
    }

    private void Update()
    {
        if (!IsOwner || !IsSpawned)
            return;

        HandleCheatInput();

        if (playerHealth != null && !playerHealth.IsAlive)
            return;

        if (playerCamera == null)
            playerCamera = Camera.main;
        if (playerCamera == null)
            return;

        HandleMoveInput();
        HandleSelectInput();
        HandleAttackInput();
        HandleSkillInput();
        HandleGiftInput();
    }

    private void HandleCheatInput()
    {
        if (!Input.GetKeyDown(KeyCode.R))
            return;

        CheatReviveServerRpc();
    }

    private void HandleMoveInput()
    {
        if (!Input.GetMouseButtonDown(1))
            return;

        if (!CwslMouseGround.TryGetGroundPoint(playerCamera, out var point, out _))
            return;

        CwslMoveDestinationMarker.Show(point);
        MoveToServerRpc(point);
    }

    private void HandleSelectInput()
    {
        if (!Input.GetMouseButtonDown(0))
            return;

        if (!CwslMouseGround.TryGetSelectableTarget(playerCamera, out var target))
        {
            ClearSelectionServerRpc();
            return;
        }

        if (target.OwnerClientId == OwnerClientId)
            return;

        SelectTargetServerRpc(new NetworkObjectReference(target));
    }

    private void HandleAttackInput()
    {
        if (!Input.GetKeyDown(KeyCode.A))
            return;

        AttackSelectedServerRpc();
    }

    private void HandleSkillInput()
    {
        var held = Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.Space);

        if (held && !skillHeld)
        {
            skillHeld = true;
            PressSkillServerRpc();
        }
        else if (!held && skillHeld)
        {
            skillHeld = false;
            ReleaseSkillServerRpc();
        }
    }

    private void HandleGiftInput()
    {
        if (goldGift == null)
            return;

        if (Input.GetKeyDown(KeyCode.G))
            goldGift.BeginHold();

        if (Input.GetKey(KeyCode.G))
            goldGift.TickHold();

        if (Input.GetKeyUp(KeyCode.G))
            goldGift.EndHold();
    }

    [ServerRpc]
    private void MoveToServerRpc(Vector3 destination)
    {
        movement?.RequestMoveTo(destination);
    }

    [ServerRpc]
    private void SelectTargetServerRpc(NetworkObjectReference targetRef)
    {
        if (!targetRef.TryGet(out var target))
            return;

        selection?.SetTargetServer(target);
    }

    [ServerRpc]
    private void ClearSelectionServerRpc()
    {
        selection?.SetTargetServer(null);
    }

    [ServerRpc]
    private void AttackSelectedServerRpc()
    {
        combat?.AttackSelectedTarget();
    }

    [ServerRpc]
    private void PressSkillServerRpc()
    {
        skills?.PressSkillServer(OwnerClientId);
    }

    [ServerRpc]
    private void ReleaseSkillServerRpc()
    {
        skills?.ReleaseSkillServer(OwnerClientId);
    }

    [ServerRpc]
    private void CastGroundSkillServerRpc(Vector3 worldPoint)
    {
        skills?.CastGroundSkillServer(OwnerClientId, worldPoint);
    }

    [ServerRpc]
    private void CheatReviveServerRpc()
    {
        playerHealth?.CheatReviveServer();
    }
}
