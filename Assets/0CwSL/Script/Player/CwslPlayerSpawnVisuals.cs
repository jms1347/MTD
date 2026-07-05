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
        else if (characterId == CwslCharacterId.CrowdGatherer)
            CwslMonsterVisualBuilder.BuildCrowdGathererPlayer(transform, color);
        else
            CwslMonsterVisualBuilder.BuildPlayer(transform, color);

        GetComponent<CwslPlayerBodyCollider>()?.ApplyForCharacter(characterId);
        EnsureRammerGallopAudio(characterId);
        EnsureTeamMemberVisible();
    }

    private void EnsureTeamMemberVisible()
    {
        if (IsOwner)
            return;

        var occludee = GetComponent<CwslVisionOccludee>();
        if (occludee != null)
            Destroy(occludee);
    }

    private void EnsureRammerGallopAudio(CwslCharacterId characterId)
    {
        var gallopRoot = transform.Find("GallopAudio");
        if (characterId != CwslCharacterId.MomentumRammer)
        {
            if (gallopRoot != null)
                Destroy(gallopRoot.gameObject);
            return;
        }

        if (gallopRoot == null)
        {
            var go = new GameObject("GallopAudio");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 0.15f, 0f);
            gallopRoot = go.transform;
        }

        var gallopAudio = gallopRoot.GetComponent<CwslPlayerHorseGallopAudio>();
        if (gallopAudio == null)
            gallopAudio = gallopRoot.gameObject.AddComponent<CwslPlayerHorseGallopAudio>();

        var clip = CwslRammerAudioFeedback.ResolveHorseGallopClip();
        if (clip != null)
            gallopAudio.AssignClip(clip);
    }
}
