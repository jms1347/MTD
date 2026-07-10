using Unity.Netcode;
using UnityEngine;

/// <summary>돌격 자폭병 — 타겟 근접 시에만 심지 VFX 활성화.</summary>
public class CwslSuicideFuseController : NetworkBehaviour
{
    private readonly NetworkVariable<bool> fuseLit = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private CwslSuicideFuseVisual fuseVisual;
    private CwslMonsterBase monsterBase;

    private void Awake()
    {
        monsterBase = GetComponent<CwslMonsterBase>();
        fuseVisual = GetComponentInChildren<CwslSuicideFuseVisual>(true);
    }

    public override void OnNetworkSpawn()
    {
        fuseLit.OnValueChanged += HandleFuseLitChanged;
        ApplyFuseLit(fuseLit.Value);
    }

    public override void OnNetworkDespawn()
    {
        fuseLit.OnValueChanged -= HandleFuseLitChanged;
        ApplyFuseLit(false);
    }

    public void ResetForPool()
    {
        if (IsServer)
            fuseLit.Value = false;
        else
            ApplyFuseLit(false);
    }

    public void SetFuseLitServer(bool lit)
    {
        if (!IsServer || UsesStickyAttachFuse())
            return;

        if (fuseLit.Value == lit)
            return;

        fuseLit.Value = lit;
    }

    private void HandleFuseLitChanged(bool previousValue, bool newValue)
    {
        ApplyFuseLit(newValue);
    }

    private void ApplyFuseLit(bool lit)
    {
        if (fuseVisual == null)
            fuseVisual = GetComponentInChildren<CwslSuicideFuseVisual>(true);

        if (fuseVisual == null)
            return;

        fuseVisual.SetBurningActive(lit);
    }

    private bool UsesStickyAttachFuse()
    {
        if (monsterBase == null)
            return GetComponent<CwslStickySuicideMonster>() != null;

        return monsterBase.MonsterType == CwslMonsterType.StickySuicide;
    }
}

