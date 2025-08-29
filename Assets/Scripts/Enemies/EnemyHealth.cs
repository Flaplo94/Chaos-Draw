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
        if (amount <= 0) return;

        currentHealth -= amount;

        // Damage numbers
        if (DamageNumbers.Instance != null)
            DamageNumbers.Instance.Show(transform.position + Vector3.up * 0.6f, amount);

        if (currentHealth <= 0)
            Die();
    }

    public void Heal(int amount)
    {
        if (amount <= 0) return;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
    }

    public int GetHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;

    void Die()
    {
        OnDeath?.Invoke();                 // lokale lyttere
        OnAnyEnemyDied?.Invoke(this);      // globalt broadcast, fx BountySystem
        Destroy(gameObject);
    }
}
