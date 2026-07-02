using UnityEngine;

[RequireComponent(typeof(Health))]
public class UnitCombatVFX : MonoBehaviour
{
    [SerializeField] private GameObject deathEffectPrefab;
    [SerializeField] private float deathEffectLifetime = 3.5f;

    private Health health;

    private void Awake()
    {
        health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        health.OnDeath += HandleDeath;
    }

    private void OnDisable()
    {
        if (health == null)
            return;

        health.OnDeath -= HandleDeath;
    }

    public void ConfigureDeathEffect(GameObject deathEffect)
    {
        deathEffectPrefab = deathEffect;
    }

    private void HandleDeath()
    {
        var prefab = deathEffectPrefab;
        if (prefab == null && DefenseCombatCatalog.Active != null)
            prefab = DefenseCombatCatalog.Active.defaultDeathEffectPrefab;

        if (prefab == null)
            return;

        SpawnEffect(prefab, transform.position + Vector3.up * 0.35f, deathEffectLifetime);
    }

    private static void SpawnEffect(GameObject prefab, Vector3 position, float lifetime, float scale = 1f)
    {
        if (prefab == null)
            return;

        GameObject effect;
        try
        {
            effect = Object.Instantiate(prefab, position, Quaternion.identity);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[UnitCombatVFX] 이펙트 생성 실패 ({prefab.name}): {ex.Message}", prefab);
            return;
        }

        if (effect == null)
            return;

        if (scale > 0f && !Mathf.Approximately(scale, 1f))
            effect.transform.localScale = Vector3.one * scale;

        Object.Destroy(effect, lifetime);
    }
}
