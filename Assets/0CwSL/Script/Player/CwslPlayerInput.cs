using Unity.Netcode;
using UnityEngine;

public class CwslPlayerInput : NetworkBehaviour
{
    private static readonly CwslCharacterId[] CharacterCycle =
    {
        CwslCharacterId.Tank,
        CwslCharacterId.MissileTank,
        CwslCharacterId.RedMage
    };

    private Camera playerCamera;
    private CwslPlayerMovement movement;
    private CwslPlayerSelection selection;
    private CwslPlayerSkills skills;
    private CwslPlayerGoldGift goldGift;
    private CwslPlayerCombat combat;
    private CwslPlayerHealth playerHealth;
    private CwslPlayerCharacter playerCharacter;

    private bool skillHeld;
    private bool groundTargeting;
    private bool attackMovePending;

    public override void OnNetworkSpawn()
    {
        movement = GetComponent<CwslPlayerMovement>();
        selection = GetComponent<CwslPlayerSelection>();
        skills = GetComponent<CwslPlayerSkills>();
        goldGift = GetComponent<CwslPlayerGoldGift>();
        combat = GetComponent<CwslPlayerCombat>();
        playerHealth = GetComponent<CwslPlayerHealth>();
        playerCharacter = GetComponent<CwslPlayerCharacter>();

        if (IsOwner)
            playerCamera = Camera.main;
    }

    private void Update()
    {
        if (!IsOwner || !IsSpawned)
            return;

        HandleCheatInput();
        HandleCharacterSwitchInput();

        if (playerHealth != null && !playerHealth.IsAlive)
        {
            CancelGroundTargeting();
            CancelAttackMovePending();
            return;
        }

        if (playerCamera == null)
            playerCamera = Camera.main;
        if (playerCamera == null)
            return;

        HandleGroundTargetPreview();
        HandleAttackMovePreview();
        HandleMoveInput();
        HandleSelectInput();
        HandleAttackInput();
        HandleSkillInput();
        HandleGiftInput();
    }

    private void HandleCharacterSwitchInput()
    {
        if (!Input.GetKeyDown(KeyCode.C) || playerCharacter == null)
            return;

        CancelGroundTargeting();
        CancelAttackMovePending();
        if (skillHeld)
        {
            skillHeld = false;
            ReleaseSkillServerRpc();
        }

        var current = playerCharacter.CharacterId;
        var index = 0;
        for (var i = 0; i < CharacterCycle.Length; i++)
        {
            if (CharacterCycle[i] == current)
            {
                index = i;
                break;
            }
        }

        var next = CharacterCycle[(index + 1) % CharacterCycle.Length];
        playerCharacter.RequestSelect(next);
    }

    private void HandleCheatInput()
    {
        if (!Input.GetKeyDown(KeyCode.R))
            return;

        CheatReviveServerRpc();
    }

    private void HandleGroundTargetPreview()
    {
        if (!groundTargeting)
        {
            if (!attackMovePending)
                CwslGroundTargetMarker.Hide();
            return;
        }

        if (CwslMouseGround.TryGetGroundPoint(playerCamera, out var point, out _))
            CwslGroundTargetMarker.Show(point);
        else
            CwslGroundTargetMarker.Hide();
    }

    private void HandleAttackMovePreview()
    {
        if (!attackMovePending || groundTargeting)
            return;

        // 어택땅 대기 중: 마우스 위치에 빨간 미리보기
        if (CwslMouseGround.TryGetGroundPoint(playerCamera, out var point, out _))
            CwslMoveDestinationMarker.ShowAttack(point);
    }

    private void HandleMoveInput()
    {
        if (!Input.GetMouseButtonDown(1))
            return;

        if (groundTargeting)
        {
            CancelGroundTargeting();
            return;
        }

        if (attackMovePending)
        {
            CancelAttackMovePending();
            return;
        }

        if (!CwslMouseGround.TryGetGroundPoint(playerCamera, out var point, out _))
            return;

        CwslMoveDestinationMarker.Show(point);
        MoveToServerRpc(point);
    }

    private void HandleSelectInput()
    {
        if (!Input.GetMouseButtonDown(0))
            return;

        // 메테오 지면 지정
        if (groundTargeting)
        {
            if (CwslMouseGround.TryGetGroundPoint(playerCamera, out var point, out _))
            {
                CastGroundSkillServerRpc(point);
                CancelGroundTargeting();
            }

            return;
        }

        // 어택땅 / 어택 유닛
        if (attackMovePending)
        {
            ResolveAttackMoveClick();
            return;
        }

        // 일반 좌클릭: 적 선택 (몬스터 우선)
        if (CwslMouseGround.TryGetSelectableTarget(playerCamera, out var target))
        {
            var monsterHealth = target.GetComponent<CwslMonsterHealth>();
            if (monsterHealth != null && monsterHealth.IsAlive)
            {
                SelectTargetServerRpc(new NetworkObjectReference(target));
                return;
            }

            var playerHealthTarget = target.GetComponent<CwslPlayerHealth>();
            if (playerHealthTarget != null && target.OwnerClientId != OwnerClientId)
            {
                SelectTargetServerRpc(new NetworkObjectReference(target));
                return;
            }
        }

        ClearSelectionServerRpc();
    }

    private void ResolveAttackMoveClick()
    {
        // 적 클릭 → 해당 적 선택 + 공격
        if (CwslMouseGround.TryGetSelectableTarget(playerCamera, out var target))
        {
            var monsterHealth = target.GetComponent<CwslMonsterHealth>();
            if (monsterHealth != null && monsterHealth.IsAlive)
            {
                CwslMoveDestinationMarker.ShowAttack(target.transform.position);
                AttackTargetServerRpc(new NetworkObjectReference(target));
                CancelAttackMovePending();
                return;
            }
        }

        // 땅 클릭 → 빨간 표시 + 어택땅 이동/공격
        if (CwslMouseGround.TryGetGroundPoint(playerCamera, out var point, out _))
        {
            CwslMoveDestinationMarker.ShowAttack(point);
            AttackMoveServerRpc(point);
            CancelAttackMovePending();
        }
    }

    private void HandleAttackInput()
    {
        if (!Input.GetKeyDown(KeyCode.A))
            return;

        // A = 어택땅/어택유닛 대기 모드
        CancelGroundTargeting();
        attackMovePending = true;
    }

    private void HandleSkillInput()
    {
        var characterId = playerCharacter != null ? playerCharacter.CharacterId : CwslCharacterId.Tank;

        if (characterId == CwslCharacterId.RedMage)
        {
            if (skillHeld)
            {
                skillHeld = false;
                ReleaseSkillServerRpc();
            }

            if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.Space))
            {
                CancelAttackMovePending();
                groundTargeting = true;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CancelGroundTargeting();
                CancelAttackMovePending();
            }

            return;
        }

        CancelGroundTargeting();

        if (characterId == CwslCharacterId.MissileTank)
        {
            if (skillHeld)
            {
                skillHeld = false;
                ReleaseSkillServerRpc();
            }

            // Q = 멀티샷 (즉시)
            if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.Space))
                AttackSelectedServerRpc(fanMode: true);

            return;
        }

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

    private void CancelGroundTargeting()
    {
        groundTargeting = false;
        CwslGroundTargetMarker.Hide();
    }

    private void CancelAttackMovePending()
    {
        attackMovePending = false;
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
    private void AttackSelectedServerRpc(bool fanMode)
    {
        combat?.AttackSelectedTarget(fanMode);
    }

    [ServerRpc]
    private void AttackTargetServerRpc(NetworkObjectReference targetRef)
    {
        if (!targetRef.TryGet(out var target))
            return;

        combat?.AttackTargetServer(target);
    }

    [ServerRpc]
    private void AttackMoveServerRpc(Vector3 destination)
    {
        combat?.BeginAttackMoveServer(destination);
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
