using UnityEngine;
using System;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 3;
    private int currentHealth;

    // Lokalt event (kun denne enemy)
    public Action OnDeath;

    // Globalt event (for alle enemies)
    public static event Action<EnemyHealth> OnAnyEnemyDied;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
    }

    public int GetHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;

    void Die()
    {
        OnDeath?.Invoke();                 // lokale lyttere
        OnAnyEnemyDied?.Invoke(this);      // globalt broadcast til fx BountySystem
        Destroy(gameObject);
    }
}
