using System.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>바리케이드 E — 넥서스 근처로 이동 후 스태미나로 수리.</summary>
public class CwslBarricadeRepairSkill : CwslPlayerSkillBase
{
    public const int BoundSlotIndex = CwslCharacterSkillCatalog.SlotE;

    private CwslPlayerHealth playerHealth;
    private CwslPlayerStun playerStun;
    private CwslPlayerSkillCooldowns skillCooldowns;
    private CwslPlayerMovement movement;
    private Coroutine repairRoutine;

    public override CwslSkillActivationType ActivationType => CwslSkillActivationType.Instant;
    public override bool IsActiveForCharacter(CwslCharacterId characterId) =>
        characterId == CwslCharacterId.Barricade;
    public override int SkillSlotIndex => BoundSlotIndex;

    public override void OnNetworkSpawn()
    {
        playerHealth = GetComponent<CwslPlayerHealth>();
        playerStun = GetComponent<CwslPlayerStun>();
        skillCooldowns = GetComponent<CwslPlayerSkillCooldowns>();
        movement = GetComponent<CwslPlayerMovement>();
    }

    public override bool CanUseSkillSlotServer(ulong senderClientId, int slotIndex, Vector3 worldPoint) =>
        slotIndex == BoundSlotIndex && CanCastServer(senderClientId);

    public override bool TryUseSkillSlotServer(ulong senderClientId, int slotIndex, Vector3 worldPoint)
    {
        if (!IsServer || slotIndex != BoundSlotIndex)
            return false;

        if (!CanCastServer(senderClientId))
            return false;

        var nexus = CwslNexus.Instance;
        if (nexus == null || !nexus.IsAlive)
            return false;

        skillCooldowns?.BeginCooldown(BoundSlotIndex);
        if (repairRoutine != null)
            StopCoroutine(repairRoutine);
        repairRoutine = StartCoroutine(RepairRoutine(nexus));
        return true;
    }

    private bool CanCastServer(ulong senderClientId)
    {
        if (!IsServer || senderClientId != OwnerClientId)
            return false;
        if (repairRoutine != null)
            return false;
        if (skillCooldowns != null && !skillCooldowns.IsReady(BoundSlotIndex))
            return false;
        if (playerHealth != null && !playerHealth.IsAlive)
            return false;
        if (playerStun != null && playerStun.IsStunned)
            return false;
        return CwslNexus.Instance != null && CwslNexus.Instance.IsAlive;
    }

    private IEnumerator RepairRoutine(CwslNexus nexus)
    {
        var approach = nexus.GetMeleeApproachPoint(transform.position, 0.4f);
        movement?.RequestMoveTo(approach);

        var timeout = 4f;
        while (timeout > 0f)
        {
            timeout -= Time.deltaTime;
            var flat = transform.position - nexus.transform.position;
            flat.y = 0f;
            if (flat.magnitude <= CwslGameConstants.BarricadeRepairRange)
                break;
            yield return null;
        }

        movement?.StopMovement();
        PlayRepairClientRpc(nexus.transform.position);
        yield return new WaitForSeconds(CwslGameConstants.BarricadeRepairDuration);

        if (nexus != null && nexus.IsAlive)
            nexus.HealServer(CwslGameConstants.BarricadeRepairAmount);

        repairRoutine = null;
    }

    [ClientRpc]
    private void PlayRepairClientRpc(Vector3 nexusPosition)
    {
        CwslVfxSpawner.SpawnBarricadeRepair(transform.position + Vector3.up * 0.8f);
        SpawnNexusRepairSparkles(nexusPosition);
        var visual = transform.Find("Visual");
        if (visual != null)
            visual.GetComponent<CwslBarricadeRepairVisual>()?.Play();
    }

    private static void SpawnNexusRepairSparkles(Vector3 nexusPosition)
    {
        var center = nexusPosition + Vector3.up * 1.2f;
        CwslVfxSpawner.SpawnBarricadeRepair(center);

        // 넥서스 수리 시 반짝임이 확실히 보이도록 주변에 추가 스파클
        var offsets = new[]
        {
            new Vector3(0.8f, 1.0f, 0f),
            new Vector3(-0.8f, 1.1f, 0.25f),
            new Vector3(0.25f, 1.25f, 0.75f),
            new Vector3(-0.2f, 0.9f, -0.7f)
        };

        for (var i = 0; i < offsets.Length; i++)
            CwslVfxSpawner.SpawnBarricadeRepair(nexusPosition + offsets[i]);
    }
}
