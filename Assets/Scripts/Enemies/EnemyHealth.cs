using UnityEngine;
using System;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 3;
    private int currentHealth;

    public Action OnDeath;

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
        OnDeath?.Invoke();

        Destroy(gameObject);
    }
}
