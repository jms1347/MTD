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
    private CwslPlayerCharacter playerCharacter;
    private CwslCrowdGatherSkill crowdGatherSkill;

    private bool skillHeld;
    private bool groundTargeting;
    private bool attackMovePending;
    private float nextRammerSteerRpcTime;

    public override void OnNetworkSpawn()
    {
        movement = GetComponent<CwslPlayerMovement>();
        selection = GetComponent<CwslPlayerSelection>();
        skills = GetComponent<CwslPlayerSkills>();
        goldGift = GetComponent<CwslPlayerGoldGift>();
        combat = GetComponent<CwslPlayerCombat>();
        playerHealth = GetComponent<CwslPlayerHealth>();
        playerCharacter = GetComponent<CwslPlayerCharacter>();
        crowdGatherSkill = GetComponent<CwslCrowdGatherSkill>();

        if (IsOwner)
            playerCamera = Camera.main;
    }

    private static bool ShouldBlockGameplayInput()
    {
        return CwslGameConstants.UseDefenseMode && CwslCharacterIntroPopup.IsVisible;
    }

    private static bool IsLocalSkillSilenced()
    {
        return NetworkManager.Singleton != null
               && CwslBossWatchState.BlocksSkills(NetworkManager.Singleton.LocalClientId);
    }

    // TODO(릴리즈): 테스트용 치트키(V/R/U) — 로비 설정으로 비활성 가능.
    private void Update()
    {
        if (!IsOwner || !IsSpawned)
            return;

        if (ShouldBlockGameplayInput())
            return;

        CwslLobbyGameSettings.EnsureLoaded();
        HandleCheatInput();

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
        HandleRammerSteerInput();
        HandleMoveInput();
        HandleSelectInput();
        HandleAttackInput();
        HandleSkillInput();
        HandleExtraSkillSlots();
        HandleGiftInput();
    }

    private void HandleExtraSkillSlots()
    {
        if (IsLocalSkillSilenced())
            return;

        var characterId = playerCharacter != null ? playerCharacter.CharacterId : CwslCharacterId.Tank;

        if (Input.GetKeyDown(KeyCode.W) && characterId == CwslCharacterId.Tank)
        {
            var dashPoint = transform.position + transform.forward * 5f;
            if (playerCamera != null && CwslMouseGround.TryGetGroundPoint(playerCamera, out var point, out _))
                dashPoint = point;

            UseSkillSlotServerRpc(3, dashPoint);
            return;
        }

        if (Input.GetKeyDown(KeyCode.W) && characterId == CwslCharacterId.MissileTank)
        {
            UseSkillSlotServerRpc(3);
            return;
        }

        if (Input.GetKeyDown(KeyCode.W) && characterId == CwslCharacterId.RedMage)
        {
            var lightningPoint = transform.position + transform.forward * 5f;
            if (playerCamera != null && CwslMouseGround.TryGetGroundPoint(playerCamera, out var point, out _))
                lightningPoint = point;

            UseSkillSlotServerRpc(3, lightningPoint);
            return;
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            var slamPoint = transform.position + transform.forward * 5f;
            if (playerCamera != null && CwslMouseGround.TryGetGroundPoint(playerCamera, out var point, out _))
                slamPoint = point;

            UseSkillSlotServerRpc(1, slamPoint);
        }
        if (Input.GetKeyDown(KeyCode.R))
            UseSkillSlotServerRpc(2);
        if (Input.GetKeyDown(KeyCode.F) && characterId != CwslCharacterId.Tank && characterId != CwslCharacterId.MissileTank && characterId != CwslCharacterId.RedMage)
            UseSkillSlotServerRpc(3);
    }

    // TODO(릴리즈): 테스트용 — V(캐릭터), R(부활), U(카르마). 로비 설정으로 비활성 가능.
    private void HandleCheatInput()
    {
        if (!CwslLobbyGameSettings.EnableDevCheats)
            return;

        if (Input.GetKeyDown(KeyCode.V))
            CheatCycleCharacterServerRpc();

        if (Input.GetKeyDown(KeyCode.R))
            CheatReviveServerRpc();

        if (Input.GetKeyDown(KeyCode.U))
            CheatAddKarmaServerRpc();
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

        if (playerCharacter != null && playerCharacter.CharacterId == CwslCharacterId.MomentumRammer)
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

    private void HandleRammerSteerInput()
    {
        if (playerCharacter == null || playerCharacter.CharacterId != CwslCharacterId.MomentumRammer)
            return;

        if (groundTargeting)
        {
            if (Input.GetMouseButtonDown(1))
                CancelGroundTargeting();
            return;
        }

        if (attackMovePending)
        {
            if (Input.GetMouseButtonDown(1))
                CancelAttackMovePending();
            return;
        }

        if (Input.GetMouseButton(1))
        {
            if (!CwslMouseGround.TryGetGroundPoint(playerCamera, out var point, out _))
                return;

            CwslMoveDestinationMarker.Show(point);
            if (Input.GetMouseButtonDown(1) || Time.time >= nextRammerSteerRpcTime)
            {
                nextRammerSteerRpcTime = Time.time + CwslGameConstants.RammerSteerRpcIntervalSeconds;
                SteerRammerServerRpc(point);
            }

            return;
        }

        if (Input.GetMouseButtonUp(1))
            ReleaseRammerSteerServerRpc();
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

            var enemyBase = target.GetComponent<CwslEnemyBase>();
            if (enemyBase != null && enemyBase.IsAlive)
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

            var enemyBase = target.GetComponent<CwslEnemyBase>();
            if (enemyBase != null && enemyBase.IsAlive)
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
        if (IsLocalSkillSilenced())
            return;

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

        if (characterId == CwslCharacterId.MomentumRammer)
        {
            CancelGroundTargeting();
            var rammerKeyHeld = Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.Space);
            if (rammerKeyHeld && !skillHeld)
            {
                skillHeld = true;
                PressSkillServerRpc();
            }
            else if (!rammerKeyHeld && skillHeld)
            {
                skillHeld = false;
                ReleaseSkillServerRpc();
            }

            return;
        }

        if (characterId == CwslCharacterId.CrowdGatherer)
        {
            CancelGroundTargeting();
            var gatherKeyHeld = Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.Space);
            if (gatherKeyHeld)
            {
                if (CwslMouseGround.TryGetGroundPoint(playerCamera, out var point, out _))
                {
                    if (!skillHeld)
                    {
                        skillHeld = true;
                        BeginGatherSkillServerRpc(point);
                    }
                    else
                    {
                        UpdateGatherSkillServerRpc(point);
                    }

                    if (crowdGatherSkill != null && crowdGatherSkill.IsCharging)
                    {
                        var previewRadius = crowdGatherSkill.ChargeRadius;
                        var atMax = crowdGatherSkill.IsAtMaxCharge;
                        CwslGatherChargeVisual.Sync(point, previewRadius, atMax);
                    }
                }
            }
            else if (skillHeld)
            {
                skillHeld = false;
                ReleaseSkillServerRpc();
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

            // Q = 양손 쌍타 (즉시)
            if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.Space))
                AttackSelectedServerRpc(dualWieldMode: true);

            return;
        }

        var skillKeyHeld = Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.Space);
        if (skillKeyHeld && !skillHeld)
        {
            skillHeld = true;
            PressSkillServerRpc();
        }
        else if (!skillKeyHeld && skillHeld)
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
        if (combat != null)
            combat.RequestMoveServer(destination);
        else
            movement?.RequestMoveTo(destination);
    }

    [ServerRpc]
    private void SteerRammerServerRpc(Vector3 worldPoint)
    {
        movement?.RequestRammerSteerTo(worldPoint);
    }

    [ServerRpc]
    private void ReleaseRammerSteerServerRpc()
    {
        movement?.ReleaseRammerSteer();
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
    private void AttackSelectedServerRpc(bool dualWieldMode)
    {
        combat?.AttackSelectedTarget(dualWieldMode);
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
    private void BeginGatherSkillServerRpc(Vector3 worldPoint)
    {
        skills?.BeginGatherSkillServer(OwnerClientId, worldPoint);
    }

    [ServerRpc]
    private void UpdateGatherSkillServerRpc(Vector3 worldPoint)
    {
        skills?.UpdateGatherSkillServer(OwnerClientId, worldPoint);
    }

    [ServerRpc]
    private void CastGroundSkillServerRpc(Vector3 worldPoint)
    {
        skills?.CastGroundSkillServer(OwnerClientId, worldPoint);
    }

    [ServerRpc]
    private void UseSkillSlotServerRpc(int slotIndex, Vector3 worldPoint = default)
    {
        skills?.UseSkillSlotServer(OwnerClientId, slotIndex, worldPoint);
    }

    [ServerRpc]
    private void CheatCycleCharacterServerRpc()
    {
        if (!CwslLobbyGameSettings.EnableDevCheats)
            return;

        playerCharacter?.CheatCycleCharacterServer();
    }

    [ServerRpc]
    private void CheatReviveServerRpc()
    {
        if (!CwslLobbyGameSettings.EnableDevCheats)
            return;

        playerHealth?.CheatReviveServer();
    }

    [ServerRpc]
    private void CheatAddKarmaServerRpc()
    {
        if (!CwslLobbyGameSettings.EnableDevCheats)
            return;

        CwslKarmaSystem.Instance?.AddKarmaServer(CwslGameConstants.CheatKarmaIncrement);
    }
}
