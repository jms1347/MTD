using Unity.Netcode;
using UnityEngine;

public class CwslPlayerVisualScale : NetworkBehaviour
{
    private readonly NetworkVariable<float> scaleMultiplier = new(
        1f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private Transform visualRoot;

    public float ScaleMultiplier => scaleMultiplier.Value;

    public override void OnNetworkSpawn()
    {
        visualRoot = transform.Find("Visual");
        scaleMultiplier.OnValueChanged += HandleScaleChanged;
        ApplyScale(scaleMultiplier.Value);
    }

    public override void OnNetworkDespawn()
    {
        scaleMultiplier.OnValueChanged -= HandleScaleChanged;
    }

    public void SetScaleServer(float multiplier)
    {
        if (!IsServer)
            return;

        scaleMultiplier.Value = Mathf.Max(0.1f, multiplier);
    }

    private void HandleScaleChanged(float previous, float current)
    {
        ApplyScale(current);
    }

    private void ApplyScale(float multiplier)
    {
        if (visualRoot != null)
            visualRoot.localScale = Vector3.one * multiplier;
    }
}
