using Unity.Netcode;
using UnityEngine;

public class CwslPlayerShieldFortifyVisual : NetworkBehaviour
{
    private readonly NetworkVariable<float> shieldScale = new(
        1f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private Transform shieldRoot;
    private float displayScale = 1f;
    private float displayVelocity;

    public override void OnNetworkSpawn()
    {
        shieldRoot = transform.Find("Visual/Shield");
        shieldScale.OnValueChanged += HandleShieldScaleChanged;
        displayScale = shieldScale.Value;
        ApplyShieldScale(displayScale);
    }

    public override void OnNetworkDespawn()
    {
        shieldScale.OnValueChanged -= HandleShieldScaleChanged;
    }

    public void SetFortifyServer(bool active)
    {
        if (!IsServer)
            return;

        shieldScale.Value = active ? CwslGameConstants.FortifyShieldScale : 1f;
    }

    private void Update()
    {
        if (shieldRoot == null)
            shieldRoot = transform.Find("Visual/Shield");

        if (shieldRoot == null)
            return;

        if (IsTankSkillVisualAnimating())
            return;

        var growing = shieldScale.Value > displayScale + 0.001f;
        var smoothTime = growing
            ? CwslGameConstants.FortifyShieldGrowSmoothTime
            : CwslGameConstants.FortifyShieldShrinkSmoothTime;

        displayScale = Mathf.SmoothDamp(
            displayScale,
            shieldScale.Value,
            ref displayVelocity,
            smoothTime,
            Mathf.Infinity,
            Time.deltaTime);
        ApplyShieldScale(displayScale);
    }

    private void HandleShieldScaleChanged(float previous, float current)
    {
        if (current < previous)
            displayVelocity = 0f;
    }

    private void ApplyShieldScale(float scale)
    {
        if (shieldRoot != null)
            shieldRoot.localScale = Vector3.one * scale;
    }

    private bool IsTankSkillVisualAnimating()
    {
        var visual = transform.Find("Visual");
        return visual != null && visual.GetComponent<CwslTankShieldSkillVisual>()?.IsAnimating == true;
    }
}
