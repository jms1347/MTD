using Unity.Netcode;
using UnityEngine;

public abstract class CwslPlayerSkillBase : NetworkBehaviour
{
    public abstract CwslSkillActivationType ActivationType { get; }

    public virtual bool IsActiveForCharacter(CwslCharacterId characterId) => true;

    public virtual bool CanCastServer(ulong senderClientId) => true;

    public virtual int SkillSlotIndex => -1;

    public virtual bool CanUseSkillSlotServer(ulong senderClientId, int slotIndex, Vector3 worldPoint) => false;

    public virtual bool TryUseSkillSlotServer(ulong senderClientId, int slotIndex, Vector3 worldPoint) => false;

    public virtual void OnSkillPressedServer(ulong senderClientId) { }
    public virtual void OnSkillReleasedServer(ulong senderClientId) { }
    public virtual void OnSkillGroundTargetServer(ulong senderClientId, Vector3 worldPoint) { }
    public virtual void TickChargedServer() { }
}
