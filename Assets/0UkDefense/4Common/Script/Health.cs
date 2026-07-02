using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private float maxHealth = 10f;
    [SerializeField] private float deathDestroyDelay = 0f;
    [SerializeField] private bool destroyOnDeath = true;

    private float currentHealth;
    private float defense;
    private float flatDefenseReduction;
    private bool isDead;

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public float Defense => defense;
    public float EffectiveDefense => Mathf.Max(0f, defense - flatDefenseReduction);
    public float DeathDestroyDelay => deathDestroyDelay;
    public bool IsAlive => !isDead && currentHealth > 0f;

    public event Action OnDeath;
    public event Action<float> OnDamaged;
    public event Action<DamageInfo> OnDamagedInfo;
    public event Action<float, float> OnHealthChanged;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void Initialize(float health, float destroyDelay = 0f, float defenseValue = 0f)
    {
        maxHealth = health;
        currentHealth = health;
        defense = Mathf.Max(0f, defenseValue);
        flatDefenseReduction = 0f;
        deathDestroyDelay = destroyDelay;
        isDead = false;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void SetDestroyOnDeath(bool destroy)
    {
        destroyOnDeath = destroy;
    }

    /// <summary>
    /// 독 등 상태 이상으로 깎이는 방어력. MonsterStatusController가 갱신합니다.
    /// </summary>
    public void SetFlatDefenseReduction(float reduction)
    {
        flatDefenseReduction = Mathf.Max(0f, reduction);
    }

    public void TakeDamage(float amount)
    {
        TakeDamage(DamageInfo.Physical(amount, transform.position));
    }

    public void TakeDamage(DamageInfo info)
    {
        if (!IsAlive || info.amount <= 0f)
            return;

        var finalDamage = Mathf.Max(1f, info.amount - EffectiveDefense);
        currentHealth = Mathf.Max(0f, currentHealth - finalDamage);

        var resolved = info;
        resolved.amount = finalDamage;
        SafeInvoke(OnDamagedInfo, resolved);
        SafeInvoke(OnDamaged, finalDamage);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0f)
            Die();
    }

    public void ApplyAuthoritativeHealth(float hp, float maxHp, float defenseValue = -1f)
    {
        if (defenseValue >= 0f)
            defense = Mathf.Max(0f, defenseValue);

        maxHealth = Mathf.Max(1f, maxHp);
        currentHealth = Mathf.Clamp(hp, 0f, maxHealth);
        isDead = currentHealth <= 0f;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
    public void Heal(float amount)
    {
        if (!IsAlive || amount <= 0f)
            return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void IncreaseMaxHealth(float amount, bool healBySameAmount = true)
    {
        if (amount <= 0f)
            return;

        maxHealth += amount;
        if (healBySameAmount)
            currentHealth += amount;
        else
            currentHealth = Mathf.Min(currentHealth, maxHealth);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private static void SafeInvoke(Action<float> handler, float value)
    {
        if (handler == null)
            return;

        foreach (var del in handler.GetInvocationList())
        {
            try
            {
                ((Action<float>)del).Invoke(value);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Health] OnDamaged handler failed: {ex.Message}");
            }
        }
    }

    private static void SafeInvoke(Action<DamageInfo> handler, DamageInfo value)
    {
        if (handler == null)
            return;

        foreach (var del in handler.GetInvocationList())
        {
            try
            {
                ((Action<DamageInfo>)del).Invoke(value);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Health] OnDamagedInfo handler failed: {ex.Message}");
            }
        }
    }

    private static void SafeInvoke(Action handler)
    {
        if (handler == null)
            return;

        foreach (var del in handler.GetInvocationList())
        {
            try
            {
                ((Action)del).Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Health] OnDeath handler failed: {ex.Message}");
            }
        }
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;
        SafeInvoke(OnDeath);

        if (destroyOnDeath)
            Destroy(gameObject, deathDestroyDelay);
    }
}
