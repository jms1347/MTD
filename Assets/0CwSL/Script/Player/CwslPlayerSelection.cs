using Unity.Netcode;
using UnityEngine;

public class CwslPlayerSelection : NetworkBehaviour
{
    private readonly NetworkVariable<NetworkObjectReference> selectedTarget = new(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private GameObject localIndicator;

    public bool TryGetSelectedTarget(out NetworkObject target)
    {
        target = null;
        if (!selectedTarget.Value.TryGet(out var obj) || obj == null)
            return false;

        target = obj;
        return true;
    }

    public override void OnNetworkSpawn()
    {
        selectedTarget.OnValueChanged += HandleSelectionChanged;
        RefreshIndicator();
    }

    public override void OnNetworkDespawn()
    {
        selectedTarget.OnValueChanged -= HandleSelectionChanged;
        DestroyIndicator();
    }

    public void SetTargetServer(NetworkObject target)
    {
        if (!IsServer)
            return;

        selectedTarget.Value = target != null ? target : default;
    }

    private void HandleSelectionChanged(NetworkObjectReference previous, NetworkObjectReference current)
    {
        RefreshIndicator();
    }

    private void RefreshIndicator()
    {
        if (!IsOwner)
            return;

        DestroyIndicator();
        if (!TryGetSelectedTarget(out var target) || target == null)
            return;

        localIndicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        localIndicator.name = "SelectionRing";
        Object.Destroy(localIndicator.GetComponent<Collider>());
        localIndicator.transform.SetParent(target.transform, false);
        var targetGrave = target.GetComponent<CwslPlayerGrave>();
        var yOffset = targetGrave != null && targetGrave.IsTombstoneActive ? 0.15f : 0.05f;
        localIndicator.transform.localPosition = new Vector3(0f, yOffset, 0f);
        localIndicator.transform.localScale = new Vector3(1.6f, 0.02f, 1.6f);
        var renderer = localIndicator.GetComponent<Renderer>();
        renderer.material = CwslMaterialUtil.CreateColored(new Color(1f, 0.92f, 0.2f, 0.85f));
    }

    private void DestroyIndicator()
    {
        if (localIndicator != null)
            Destroy(localIndicator);
        localIndicator = null;
    }
}
