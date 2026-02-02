using UnityEngine;

public class HealthSystem : MonoBehaviour
{
    [SerializeField] private int maxHealth = 50;
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
    }
}
