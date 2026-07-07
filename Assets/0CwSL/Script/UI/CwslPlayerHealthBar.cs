using Unity.Netcode;
using UnityEngine;

public class CwslPlayerHealthBar : NetworkBehaviour
{
    private const float BarWidth = 1.35f;
    private const float BarHeight = 0.12f;
    private const float HeightOffset = 2.35f;

    private CwslPlayerHealth playerHealth;
    private Transform barRoot;
    private Transform fillTransform;
    private Renderer fillRenderer;

    public override void OnNetworkSpawn()
    {
        playerHealth = GetComponent<CwslPlayerHealth>();
        EnsureBarVisual();
        if (playerHealth != null)
            playerHealth.OnHealthChanged += HandleHealthChanged;

        RefreshBar();
        HandleHealthChanged(playerHealth != null ? playerHealth.CurrentHealth : CwslGameConstants.PlayerMaxHealth);
    }

    public override void OnNetworkDespawn()
    {
        if (playerHealth != null)
            playerHealth.OnHealthChanged -= HandleHealthChanged;
    }

    private void LateUpdate()
    {
        if (barRoot == null)
            return;

        var showBar = playerHealth != null && playerHealth.IsAlive;
        if (barRoot.gameObject.activeSelf != showBar)
            barRoot.gameObject.SetActive(showBar);

        if (!showBar)
            return;

        barRoot.position = transform.position + Vector3.up * HeightOffset;
    }

    private void HandleHealthChanged(float currentHealth)
    {
        RefreshBar(currentHealth);
    }

    private void RefreshBar(float? currentHealth = null)
    {
        if (fillTransform == null)
            return;

        var current = currentHealth ?? (playerHealth != null ? playerHealth.CurrentHealth : CwslGameConstants.PlayerMaxHealth);
        var maxHealth = playerHealth != null ? playerHealth.MaxHealth : CwslGameConstants.PlayerMaxHealth;
        var ratio = maxHealth > 0f ? Mathf.Clamp01(current / maxHealth) : 0f;
        fillTransform.localScale = new Vector3(BarWidth * ratio, BarHeight, 0.1f);
        fillTransform.localPosition = new Vector3(-BarWidth * 0.5f + (BarWidth * ratio * 0.5f), 0f, -0.01f);

        if (fillRenderer != null)
        {
            var color = Color.Lerp(new Color(0.95f, 0.2f, 0.2f), new Color(0.25f, 0.9f, 0.35f), ratio);
            fillRenderer.material.color = color;
        }
    }

    private void EnsureBarVisual()
    {
        if (barRoot != null)
            return;

        var rootObject = new GameObject("HealthBar");
        rootObject.transform.SetParent(transform, false);
        barRoot = rootObject.transform;
        barRoot.gameObject.AddComponent<CwslBillboardToCamera>();

        var back = GameObject.CreatePrimitive(PrimitiveType.Cube);
        back.name = "HealthBar_Back";
        back.transform.SetParent(barRoot, false);
        back.transform.localScale = new Vector3(BarWidth, BarHeight, 0.08f);
        Object.Destroy(back.GetComponent<Collider>());
        CwslMaterialUtil.ApplyColor(back.GetComponent<Renderer>(), new Color(0.12f, 0.12f, 0.14f, 0.9f));

        var fill = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fill.name = "HealthBar_Fill";
        fill.transform.SetParent(barRoot, false);
        fill.transform.localScale = new Vector3(BarWidth, BarHeight, 0.1f);
        Object.Destroy(fill.GetComponent<Collider>());
        fillRenderer = fill.GetComponent<Renderer>();
        CwslMaterialUtil.ApplyColor(fillRenderer, new Color(0.25f, 0.9f, 0.35f));
        fillTransform = fill.transform;
    }
}
