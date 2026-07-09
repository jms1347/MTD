using Unity.Netcode;
using UnityEngine;

public class CwslPlayerFortifyVfx : NetworkBehaviour
{
    private CwslTankFortifySkill fortifySkill;
    private CwslPlayerCharacter playerCharacter;
    private GameObject auraInstance;
    private bool wasFortifying;

    public override void OnNetworkSpawn()
    {
        fortifySkill = GetComponent<CwslTankFortifySkill>();
        playerCharacter = GetComponent<CwslPlayerCharacter>();
    }

    private void Update()
    {
        // Q 홀드 중 ShieldSoftBlue 오라 표시.
        var fortifying = fortifySkill != null &&
                         playerCharacter != null &&
                         playerCharacter.CharacterId == CwslCharacterId.Tank &&
                         fortifySkill.IsFortifying;

        if (fortifying == wasFortifying)
            return;

        wasFortifying = fortifying;
        if (fortifying)
            ShowAura();
        else
            HideAura();
    }

    private void ShowAura()
    {
        HideAura();
        auraInstance = CwslVfxSpawner.SpawnFortifyAura(transform);
    }

    private void HideAura()
    {
        if (auraInstance != null)
            Destroy(auraInstance);
        auraInstance = null;
    }

    public override void OnNetworkDespawn()
    {
        HideAura();
    }
}
