using Unity.Netcode;
using UnityEngine;

/// <summary>바리케이드 Q — 드래그 시작~끝 직선 벽 설치.</summary>
public class CwslBarricadeWallSkill : CwslPlayerSkillBase
{
    public const int BoundSlotIndex = CwslCharacterSkillCatalog.SlotQ;

    private CwslPlayerHealth playerHealth;
    private CwslPlayerStun playerStun;
    private CwslPlayerSkillCooldowns skillCooldowns;
    private CwslPlayerCharacter playerCharacter;

    public override CwslSkillActivationType ActivationType => CwslSkillActivationType.Instant;

    public override bool IsActiveForCharacter(CwslCharacterId characterId) =>
        characterId == CwslCharacterId.Barricade;

    public override int SkillSlotIndex => BoundSlotIndex;

    public override void OnNetworkSpawn()
    {
        playerHealth = GetComponent<CwslPlayerHealth>();
        playerStun = GetComponent<CwslPlayerStun>();
        skillCooldowns = GetComponent<CwslPlayerSkillCooldowns>();
        playerCharacter = GetComponent<CwslPlayerCharacter>();
    }

    public override bool CanUseSkillSlotServer(ulong senderClientId, int slotIndex, Vector3 worldPoint) =>
        false; // Q는 drag 전용 ServerRpc로 처리

    public bool CanCastServer(ulong senderClientId)
    {
        if (!IsServer || senderClientId != OwnerClientId)
            return false;

        if (playerCharacter == null || playerCharacter.CharacterId != CwslCharacterId.Barricade)
            return false;

        if (skillCooldowns != null && !skillCooldowns.IsReady(BoundSlotIndex))
            return false;

        if (playerHealth != null && !playerHealth.IsAlive)
            return false;

        if (playerStun != null && playerStun.IsStunned)
            return false;

        return true;
    }

    public bool TryBuildWallServer(ulong senderClientId, Vector3 start, Vector3 end)
    {
        if (!CanCastServer(senderClientId))
            return false;

        start.y = 0f;
        end.y = 0f;
        var length = Vector3.Distance(start, end);
        if (length < CwslGameConstants.BarricadeWallMinLength)
            return false;

        var skills = GetComponent<CwslPlayerSkills>();
        if (skills != null && !skills.TrySpendStaminaForSlot(BoundSlotIndex))
            return false;

        skillCooldowns?.BeginCooldown(BoundSlotIndex);
        var wall = CwslBarricadeWall.SpawnServer(
            start,
            end,
            OwnerClientId,
            CwslGameConstants.BarricadeWallMaxHealth);
        if (wall == null)
            return false;

        PlayBuildClientRpc(start, end);
        return true;
    }

    [ClientRpc]
    private void PlayBuildClientRpc(Vector3 start, Vector3 end)
    {
        var mid = (start + end) * 0.5f;
        mid.y = 0.2f;
        CwslVfxSpawner.SpawnBarricadeBuildDust(mid);

        if (IsServer)
            return;

        SpawnClientWallVisual(start, end);
    }

    private static void SpawnClientWallVisual(Vector3 start, Vector3 end)
    {
        start.y = 0f;
        end.y = 0f;
        var delta = end - start;
        delta.y = 0f;
        var length = Mathf.Clamp(delta.magnitude, CwslGameConstants.BarricadeWallMinLength, CwslGameConstants.BarricadeWallMaxLength);
        if (delta.sqrMagnitude < 0.0001f)
            return;

        var direction = delta.normalized;
        var center = start + direction * (length * 0.5f);
        center.y = CwslGameConstants.BarricadeWallHeight * 0.5f;

        var go = new GameObject("BarricadeWallVisual");
        go.transform.position = center;
        go.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        go.transform.localScale = new Vector3(
            CwslGameConstants.BarricadeWallThickness,
            CwslGameConstants.BarricadeWallHeight,
            length);

        var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.transform.SetParent(go.transform, false);
        Object.Destroy(body.GetComponent<Collider>());
        var renderer = body.GetComponent<Renderer>();
        if (renderer != null)
            CwslMaterialUtil.ApplyColor(renderer, new Color(0.58f, 0.4f, 0.3f));

        CwslBarricadeWallClientVisuals.Register(go, center);
    }

    [ClientRpc]
    public void NotifyWallDestroyedClientRpc(Vector3 flatCenter, bool detonated)
    {
        if (IsServer)
            return;

        if (detonated)
            CwslVfxSpawner.SpawnBarricadeDetonate(flatCenter);
        CwslBarricadeWallClientVisuals.DestroyNear(flatCenter);
    }
}

/// <summary>비서버 클라의 바리케이드 벽 비주얼 목록.</summary>
public static class CwslBarricadeWallClientVisuals
{
    private static readonly System.Collections.Generic.List<(GameObject go, Vector3 center)> entries = new(16);

    public static void Register(GameObject go, Vector3 center)
    {
        if (go == null)
            return;
        center.y = 0f;
        entries.Add((go, center));
    }

    public static void DestroyNear(Vector3 flatCenter, float maxDistance = 1.25f)
    {
        flatCenter.y = 0f;
        var maxSq = maxDistance * maxDistance;
        for (var i = entries.Count - 1; i >= 0; i--)
        {
            var entry = entries[i];
            if (entry.go == null)
            {
                entries.RemoveAt(i);
                continue;
            }

            var c = entry.center;
            c.y = 0f;
            if ((c - flatCenter).sqrMagnitude > maxSq)
                continue;

            Object.Destroy(entry.go);
            entries.RemoveAt(i);
        }
    }
}
