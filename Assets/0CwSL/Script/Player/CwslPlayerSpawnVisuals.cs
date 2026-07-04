using Unity.Netcode;
using UnityEngine;

public class CwslPlayerSpawnVisuals : NetworkBehaviour
{
    private static readonly Color[] Palette =
    {
        new(0.25f, 0.65f, 0.95f),
        new(0.95f, 0.45f, 0.25f),
        new(0.45f, 0.9f, 0.45f),
        new(0.9f, 0.4f, 0.75f),
        new(0.95f, 0.85f, 0.25f)
    };

    private CwslPlayerCharacter playerCharacter;

    public override void OnNetworkSpawn()
    {
        playerCharacter = GetComponent<CwslPlayerCharacter>();
        if (playerCharacter != null)
            playerCharacter.OnCharacterChanged += HandleCharacterChanged;

        RebuildVisual();
    }

    public override void OnNetworkDespawn()
    {
        if (playerCharacter != null)
            playerCharacter.OnCharacterChanged -= HandleCharacterChanged;
    }

    private void HandleCharacterChanged(CwslCharacterId characterId)
    {
        RebuildVisual();
    }

    private void RebuildVisual()
    {
        var existing = transform.Find("Visual");
        if (existing != null)
            Destroy(existing.gameObject);

        var color = Palette[OwnerClientId % (ulong)Palette.Length];
        var characterId = playerCharacter != null ? playerCharacter.CharacterId : CwslCharacterId.Tank;

        if (characterId == CwslCharacterId.MissileTank)
            CwslMonsterVisualBuilder.BuildMissileTankPlayer(transform, color);
        else if (characterId == CwslCharacterId.RedMage)
            CwslMonsterVisualBuilder.BuildRedMagePlayer(transform, new Color(0.9f, 0.15f, 0.1f));
        else if (characterId == CwslCharacterId.MomentumRammer)
            CwslMonsterVisualBuilder.BuildMomentumRammerPlayer(transform, color);
        else
            CwslMonsterVisualBuilder.BuildPlayer(transform, color);
    }
}
