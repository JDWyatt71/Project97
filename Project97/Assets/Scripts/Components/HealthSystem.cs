using System;
using UnityEngine;

public class HealthSystem : MonoBehaviour
{
    public event Action RunningIsFalse;

    [SerializeField] private int maxHealth = 50;
    public int GetMaxHealth()
    {
        return maxHealth;
    }
    public void IncreaseMaxHealth(int amount)
    {
        maxHealth += amount;
    }
    /// <summary>
    /// Sets both max and current health to value.
    /// </summary>
    /// <param name="amount"></param>
    public void Setup(int amount)
    {
        maxHealth = amount;
        currentHealth = amount;
    }
    private int currentHealth;
    public int GetHealth()
    {
        return currentHealth;
    }

    public delegate void OnHealthChanged(int current, int max);
    public event OnHealthChanged HealthChanged;

    void Awake()
    {
        currentHealth = maxHealth;
    }
    /// <summary>
    /// Does damage, and returns true if this health system is dead
    /// </summary>
    /// <param name="amount"></param>
    /// <returns></returns>
    public bool TakeDamage(int amount)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        HealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
            return true;
        }

        
        return false;
    }

    public void Heal(int amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        HealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Die()
    {
        Destroy(gameObject);
        RunningIsFalse?.Invoke();
    }
}
