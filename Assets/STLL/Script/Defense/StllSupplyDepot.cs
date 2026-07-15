using Unity.Netcode;
using UnityEngine;

/// <summary>군량고 — 파괴 시 런 실패 카운트.</summary>
public class StllSupplyDepot : NetworkBehaviour
{
    public const float DefaultMaxHealth = 4000f;
    public const float BarHeight = 4.2f;

    private readonly NetworkVariable<float> health = new(
        DefaultMaxHealth,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<float> maxHealth = new(
        DefaultMaxHealth,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<byte> depotLabel = new(
        (byte)'A',
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public char Label => (char)depotLabel.Value;
    public float CurrentHealth => health.Value;
    public float MaxHealth => maxHealth.Value;
    public bool IsAlive => health.Value > 0f;

    public static event System.Action<StllSupplyDepot> OnAnyDestroyed;

    private bool visualBuilt;

    public override void OnNetworkSpawn()
    {
        if (depotLabel.Value != 0)
            EnsureVisual((char)depotLabel.Value);
    }

    public void ConfigureServer(char label, float maxHp, Vector3 worldPosition)
    {
        if (!IsServer)
            return;

        depotLabel.Value = (byte)label;
        maxHealth.Value = maxHp;
        health.Value = maxHp;
        transform.position = worldPosition;
        EnsureVisual(label);
    }

    public void DamageServer(float amount)
    {
        if (!IsServer || !IsAlive)
            return;

        health.Value = Mathf.Max(0f, health.Value - amount);
        if (health.Value <= 0f)
        {
            OnAnyDestroyed?.Invoke(this);
            NetworkObject.Despawn(true);
        }
    }

    private void EnsureVisual(char label)
    {
        if (visualBuilt)
            return;

        visualBuilt = true;
        BuildVisual(label);
    }

    private void BuildVisual(char label)
    {
        var profile = StllRoleVisualCatalog.Get(StllBrotherhoodRole.GuanYu);
        var bodyColor = new Color(0.55f, 0.42f, 0.22f);
        var roofColor = new Color(0.38f, 0.28f, 0.16f);

        StllVisualUtil.CreatePrimitive(PrimitiveType.Cube, transform, new Vector3(0f, 1.2f, 0f),
            new Vector3(2.4f, 2.4f, 2.4f), bodyColor);
        StllVisualUtil.CreatePrimitive(PrimitiveType.Cube, transform, new Vector3(0f, 2.6f, 0f),
            new Vector3(2.8f, 0.5f, 2.8f), roofColor);
        StllVisualUtil.CreatePrimitive(PrimitiveType.Cylinder, transform, new Vector3(0f, 3.4f, 0f),
            new Vector3(0.12f, 0.5f, 0.12f), profile.BannerColor);

        var labelRoot = new GameObject("Label").transform;
        labelRoot.SetParent(transform, false);
        labelRoot.localPosition = new Vector3(0f, 3.8f, 0f);
        StllVisualUtil.CreatePrimitive(PrimitiveType.Cube, labelRoot, Vector3.zero,
            new Vector3(0.6f, 0.6f, 0.08f), profile.AccentColor);

        var collider = gameObject.AddComponent<BoxCollider>();
        collider.size = new Vector3(2.6f, 2.8f, 2.6f);
        collider.center = new Vector3(0f, 1.4f, 0f);
    }
}
