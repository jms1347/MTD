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
    private CwslBarricadeWallSkill barricadeWallSkill;

    private bool skillHeld;
    private bool groundTargeting;
    private bool rammerRopeTargeting;
    private bool rammerRopeCancelLatch;
    private bool gathererSwapTargeting;
    private bool attackMovePending;
    private bool barricadeWallDragging;
    private Vector3 barricadeWallDragStart;
    private float nextRammerSteerRpcTime;
    private Vector3 lastGroundTargetPoint;
    private bool hasLastGroundTargetPoint;
    private Vector3 lastRammerRopeTargetPoint;
    private bool hasLastRammerRopeTargetPoint;
    private Vector3 lastGathererSwapTargetPoint;
    private bool hasLastGathererSwapTargetPoint;
    private const float RammerRopePreviewRadiusScale = 1.15f;

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
        barricadeWallSkill = GetComponent<CwslBarricadeWallSkill>();

        if (IsOwner)
            playerCamera = Camera.main;

        if (playerCharacter != null)
            playerCharacter.OnCharacterChanged += HandleCharacterChanged;
    }

    public override void OnNetworkDespawn()
    {
        if (playerCharacter != null)
            playerCharacter.OnCharacterChanged -= HandleCharacterChanged;
    }

    private void HandleCharacterChanged(CwslCharacterId characterId)
    {
        if (!IsOwner)
            return;

        skillHeld = false;
        groundTargeting = false;
        rammerRopeTargeting = false;
        rammerRopeCancelLatch = false;
        gathererSwapTargeting = false;
        attackMovePending = false;
        barricadeWallDragging = false;
        hasLastGroundTargetPoint = false;
        hasLastRammerRopeTargetPoint = false;
        hasLastGathererSwapTargetPoint = false;
        CwslRammerRopeTargetMarker.Hide();
        CwslGathererSwapRegionMarker.Hide();
        CwslGroundTargetMarker.Hide();
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
            CancelRammerRopeTargeting();
            CancelGathererSwapTargeting();
            CancelAttackMovePending();
            return;
        }

        if (playerCamera == null)
            playerCamera = Camera.main;
        if (playerCamera == null)
            return;

        HandleRammerRopeHoldInput();
        HandleGathererSwapInput();
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
            var movement = GetComponent<CwslPlayerMovement>();
            var dashPoint = movement != null && movement.TryGetFlatMoveDirection(out var moveDirection)
                ? transform.position + moveDirection * 5f
                : transform.position + transform.forward * 5f;

            UseSkillSlotServerRpc(CwslCharacterSkillCatalog.SlotW, dashPoint);
            return;
        }

        if (Input.GetKeyDown(KeyCode.W) && characterId == CwslCharacterId.MissileTank)
        {
            UseSkillSlotServerRpc(CwslCharacterSkillCatalog.SlotW);
            return;
        }

        if (Input.GetKeyDown(KeyCode.W) && characterId == CwslCharacterId.RedMage)
        {
            var lightningPoint = transform.position + transform.forward * 5f;
            if (playerCamera != null && CwslMouseGround.TryGetGroundPoint(playerCamera, out var point, out _))
                lightningPoint = point;

            UseSkillSlotServerRpc(CwslCharacterSkillCatalog.SlotW, lightningPoint);
            return;
        }

        if (Input.GetKeyDown(KeyCode.W) &&
            (characterId == CwslCharacterId.MomentumRammer ||
             characterId == CwslCharacterId.Barricade ||
             characterId == CwslCharacterId.Healer))
        {
            var skillPoint = transform.position + transform.forward * 5f;
            if (playerCamera != null && CwslMouseGround.TryGetGroundPoint(playerCamera, out var point, out _))
                skillPoint = point;

            UseSkillSlotServerRpc(CwslCharacterSkillCatalog.SlotW, skillPoint);
            return;
        }

        if (Input.GetKeyDown(KeyCode.W) && characterId == CwslCharacterId.CrowdGatherer)
        {
            var skillPoint = transform.position;
            skillPoint.y = 0f;
            if (playerCamera != null && CwslMouseGround.TryGetSkillGroundPoint(playerCamera, out var point))
            {
                skillPoint = point;
                skillPoint.y = 0f;
            }

            UseSkillSlotServerRpc(CwslCharacterSkillCatalog.SlotW, skillPoint);
            return;
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (characterId == CwslCharacterId.MomentumRammer ||
                characterId == CwslCharacterId.CrowdGatherer)
                return;

            var slamPoint = transform.position + transform.forward * 5f;
            if (playerCamera != null && CwslMouseGround.TryGetGroundPoint(playerCamera, out var point, out _))
                slamPoint = point;

            UseSkillSlotServerRpc(CwslCharacterSkillCatalog.SlotE, slamPoint);
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            var skillPoint = transform.position + transform.forward * 5f;
            if (playerCamera != null && CwslMouseGround.TryGetGroundPoint(playerCamera, out var point, out _))
                skillPoint = point;

            UseSkillSlotServerRpc(CwslCharacterSkillCatalog.SlotR, skillPoint);
        }
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
        if (gathererSwapTargeting)
        {
            var radius = CwslGameConstants.GathererSwapRegionRadius;
            var playerCenter = transform.position;
            playerCenter.y = 0f;

            if (CwslMouseGround.TryGetSkillGroundPoint(playerCamera, out var swapPoint))
            {
                lastGathererSwapTargetPoint = swapPoint;
                hasLastGathererSwapTargetPoint = true;
            }

            if (hasLastGathererSwapTargetPoint)
            {
                CwslGathererSwapRegionMarker.Show(
                    lastGathererSwapTargetPoint,
                    playerCenter,
                    radius);
            }

            return;
        }

        CwslGathererSwapRegionMarker.Hide();

        if (rammerRopeTargeting)
        {
            var radius = CwslGameConstants.RammerRopeLinkRadius * RammerRopePreviewRadiusScale;
            if (CwslMouseGround.TryGetSkillGroundPoint(playerCamera, out var ropePoint))
            {
                lastRammerRopeTargetPoint = ropePoint;
                hasLastRammerRopeTargetPoint = true;
                CwslRammerRopeTargetMarker.Show(ropePoint, radius);
            }
            else if (hasLastRammerRopeTargetPoint)
            {
                CwslRammerRopeTargetMarker.Show(lastRammerRopeTargetPoint, radius);
            }

            return;
        }

        CwslRammerRopeTargetMarker.Hide();

        if (!groundTargeting)
        {
            if (!attackMovePending)
                CwslGroundTargetMarker.Hide();
            return;
        }

        if (CwslMouseGround.TryGetSkillGroundPoint(playerCamera, out var point))
        {
            lastGroundTargetPoint = point;
            hasLastGroundTargetPoint = true;
            CwslGroundTargetMarker.Show(lastGroundTargetPoint);
        }
        else if (hasLastGroundTargetPoint)
        {
            CwslGroundTargetMarker.Show(lastGroundTargetPoint);
        }
        else
        {
            CwslGroundTargetMarker.Hide();
        }
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

        if (rammerRopeTargeting)
        {
            CancelRammerRopeTargeting(latchUntilERelease: true);
            return;
        }

        if (gathererSwapTargeting)
        {
            CancelGathererSwapTargeting();
            return;
        }

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

        if (rammerRopeTargeting)
        {
            if (Input.GetMouseButtonDown(1))
                CancelRammerRopeTargeting(latchUntilERelease: true);
            return;
        }

        if (gathererSwapTargeting)
        {
            if (Input.GetMouseButtonDown(1))
                CancelGathererSwapTargeting();
            return;
        }

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

        if (rammerRopeTargeting)
        {
            if (CwslMouseGround.TryGetSkillGroundPoint(playerCamera, out var ropePoint))
            {
                lastRammerRopeTargetPoint = ropePoint;
                hasLastRammerRopeTargetPoint = true;
            }

            if (hasLastRammerRopeTargetPoint)
            {
                UseSkillSlotServerRpc(CwslCharacterSkillCatalog.SlotE, lastRammerRopeTargetPoint);
                CancelRammerRopeTargeting();
            }

            return;
        }

        if (gathererSwapTargeting)
        {
            if (CwslMouseGround.TryGetSkillGroundPoint(playerCamera, out var swapPoint))
            {
                lastGathererSwapTargetPoint = swapPoint;
                hasLastGathererSwapTargetPoint = true;
            }

            if (hasLastGathererSwapTargetPoint)
            {
                UseSkillSlotServerRpc(CwslCharacterSkillCatalog.SlotE, lastGathererSwapTargetPoint);
                CancelGathererSwapTargeting();
            }

            return;
        }

        // 메테오 지면 지정
        if (groundTargeting)
        {
            if (CwslMouseGround.TryGetSkillGroundPoint(playerCamera, out var point))
            {
                lastGroundTargetPoint = point;
                hasLastGroundTargetPoint = true;
            }

            if (hasLastGroundTargetPoint)
            {
                CastGroundSkillServerRpc(lastGroundTargetPoint);
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

        // 일반 좌클릭: 실제로 클릭한 대상 선택 (아군/적 모두)
        if (CwslMouseGround.TryGetGroundPoint(playerCamera, out _, out var hitCollider) &&
            TryResolveSelectableFromCollider(hitCollider, out var target))
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

        // 땅 좌클릭: 현재 선택 해제
        ClearSelectionServerRpc();
    }

    private bool TryResolveSelectableFromCollider(Collider hitCollider, out NetworkObject target)
    {
        target = null;
        if (hitCollider == null)
            return false;

        var monsterHealth = hitCollider.GetComponentInParent<CwslMonsterHealth>();
        if (monsterHealth != null && monsterHealth.IsAlive && monsterHealth.NetworkObject != null)
        {
            target = monsterHealth.NetworkObject;
            return true;
        }

        var enemyBase = hitCollider.GetComponentInParent<CwslEnemyBase>();
        if (enemyBase != null && enemyBase.IsAlive && enemyBase.NetworkObject != null)
        {
            target = enemyBase.NetworkObject;
            return true;
        }

        var playerHealthTarget = hitCollider.GetComponentInParent<CwslPlayerHealth>();
        if (playerHealthTarget != null &&
            playerHealthTarget.NetworkObject != null &&
            playerHealthTarget.OwnerClientId != OwnerClientId &&
            playerHealthTarget.IsAlive)
        {
            target = playerHealthTarget.NetworkObject;
            return true;
        }

        return false;
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
                if (playerCamera != null && CwslMouseGround.TryGetSkillGroundPoint(playerCamera, out var point))
                {
                    lastGroundTargetPoint = point;
                    hasLastGroundTargetPoint = true;
                }
                else
                {
                    lastGroundTargetPoint = transform.position + transform.forward * 4f;
                    lastGroundTargetPoint.y = 0f;
                    hasLastGroundTargetPoint = true;
                }
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
            if (Input.GetKeyDown(KeyCode.Escape))
                CancelRammerRopeTargeting(latchUntilERelease: true);
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
                CancelGathererSwapTargeting();
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
                        CwslGatherChargeVisual.SyncBlackHoleZone(
                            point,
                            crowdGatherSkill.ChargeRadius);
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

        if (characterId == CwslCharacterId.Barricade)
        {
            CancelGroundTargeting();
            var wallKeyHeld = Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.Space);
            if (wallKeyHeld)
            {
                if (CwslMouseGround.TryGetGroundPoint(playerCamera, out var point, out _))
                {
                    if (!barricadeWallDragging)
                    {
                        barricadeWallDragging = true;
                        skillHeld = true;
                        barricadeWallDragStart = point;
                    }
                    CwslGroundTargetMarker.ShowLine(
                        barricadeWallDragStart,
                        point,
                        CwslGameConstants.BarricadeWallThickness);
                }
            }
            else if (barricadeWallDragging)
            {
                barricadeWallDragging = false;
                skillHeld = false;
                CwslGroundTargetMarker.Hide();
                var endPoint = barricadeWallDragStart + transform.forward * CwslGameConstants.BarricadeWallMinLength;
                if (CwslMouseGround.TryGetGroundPoint(playerCamera, out var groundPoint, out _))
                    endPoint = groundPoint;

                BuildBarricadeWallServerRpc(barricadeWallDragStart, endPoint);
            }

            if (Input.GetKeyDown(KeyCode.Escape) && barricadeWallDragging)
            {
                barricadeWallDragging = false;
                skillHeld = false;
                CwslGroundTargetMarker.Hide();
            }

            return;
        }

        if (characterId == CwslCharacterId.Healer)
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
                if (playerCamera != null && CwslMouseGround.TryGetSkillGroundPoint(playerCamera, out var point))
                {
                    lastGroundTargetPoint = point;
                    hasLastGroundTargetPoint = true;
                }
                else
                {
                    lastGroundTargetPoint = transform.position + transform.forward * 4f;
                    lastGroundTargetPoint.y = 0f;
                    hasLastGroundTargetPoint = true;
                }
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
        hasLastGroundTargetPoint = false;
        CwslGroundTargetMarker.Hide();
    }

    private void HandleRammerRopeHoldInput()
    {
        if (IsLocalSkillSilenced())
            return;

        var characterId = playerCharacter != null ? playerCharacter.CharacterId : CwslCharacterId.Tank;
        if (characterId != CwslCharacterId.MomentumRammer)
            return;

        var eKeyHeld = Input.GetKey(KeyCode.E);
        if (!eKeyHeld)
        {
            rammerRopeCancelLatch = false;
            if (rammerRopeTargeting)
                CancelRammerRopeTargeting();
            return;
        }

        if (rammerRopeCancelLatch)
            return;

        if (!rammerRopeTargeting)
        {
            CancelGroundTargeting();
            CancelAttackMovePending();
            rammerRopeTargeting = true;
            var radius = CwslGameConstants.RammerRopeLinkRadius * RammerRopePreviewRadiusScale;
            if (CwslMouseGround.TryGetSkillGroundPoint(playerCamera, out var ropePoint))
            {
                lastRammerRopeTargetPoint = ropePoint;
                hasLastRammerRopeTargetPoint = true;
                CwslRammerRopeTargetMarker.Show(ropePoint, radius);
            }
            else
            {
                lastRammerRopeTargetPoint = transform.position + transform.forward * 4f;
                lastRammerRopeTargetPoint.y = 0f;
                hasLastRammerRopeTargetPoint = true;
                CwslRammerRopeTargetMarker.Show(lastRammerRopeTargetPoint, radius);
            }
        }
    }

    private void HandleGathererSwapInput()
    {
        if (IsLocalSkillSilenced())
            return;

        var characterId = playerCharacter != null ? playerCharacter.CharacterId : CwslCharacterId.Tank;
        if (characterId != CwslCharacterId.CrowdGatherer)
            return;

        if (Input.GetKeyDown(KeyCode.Escape) && gathererSwapTargeting)
        {
            CancelGathererSwapTargeting();
            return;
        }

        if (!Input.GetKeyDown(KeyCode.E))
            return;

        if (!gathererSwapTargeting)
        {
            CancelGroundTargeting();
            CancelAttackMovePending();
            CancelRammerRopeTargeting();
            gathererSwapTargeting = true;
            var radius = CwslGameConstants.GathererSwapRegionRadius;
            var playerCenter = transform.position;
            playerCenter.y = 0f;

            if (CwslMouseGround.TryGetSkillGroundPoint(playerCamera, out var swapPoint))
            {
                lastGathererSwapTargetPoint = swapPoint;
                hasLastGathererSwapTargetPoint = true;
            }
            else
            {
                lastGathererSwapTargetPoint = transform.position + transform.forward * 4f;
                lastGathererSwapTargetPoint.y = 0f;
                hasLastGathererSwapTargetPoint = true;
            }

            CwslGathererSwapRegionMarker.Show(
                lastGathererSwapTargetPoint,
                playerCenter,
                radius);
            return;
        }

        if (hasLastGathererSwapTargetPoint)
        {
            UseSkillSlotServerRpc(CwslCharacterSkillCatalog.SlotE, lastGathererSwapTargetPoint);
            CancelGathererSwapTargeting();
        }
    }

    private void CancelGathererSwapTargeting()
    {
        gathererSwapTargeting = false;
        hasLastGathererSwapTargetPoint = false;
        CwslGathererSwapRegionMarker.Hide();
    }

    private void CancelRammerRopeTargeting(bool latchUntilERelease = false)
    {
        rammerRopeTargeting = false;
        hasLastRammerRopeTargetPoint = false;
        if (latchUntilERelease)
            rammerRopeCancelLatch = true;
        CwslRammerRopeTargetMarker.Hide();
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
    private void BuildBarricadeWallServerRpc(Vector3 start, Vector3 end)
    {
        if (skills != null && skills.BlocksSkillUseForOwner())
            return;

        GetComponent<CwslBarricadeWallSkill>()?.TryBuildWallServer(OwnerClientId, start, end);
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
