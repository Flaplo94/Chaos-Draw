using System;
using UnityEngine;

[RequireComponent(typeof(HitFlash))]
public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private bool flashOnLethalHit = true;

    private int currentHealth;
    private HitFlash flash;

    public Action OnDeath;

    void Awake()
    {
        currentHealth = maxHealth;
        flash = GetComponent<HitFlash>() ?? GetComponentInChildren<HitFlash>(true);
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;

        if (flashOnLethalHit || currentHealth > 0)
            flash?.PlayFlash();

        if (currentHealth <= 0)
            Die();
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
