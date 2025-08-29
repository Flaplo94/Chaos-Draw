using System;
using UnityEngine;

[RequireComponent(typeof(HitFlash))]
public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private bool flashOnLethalHit = true;

    private int currentHealth;
    private HitFlash flash;
    private EnemyAnimator enemyAnimator;
    public Action OnDeath;
    private CircleCollider2D hitbox;

    // Globalt event (for alle enemies)
    public static event Action<EnemyHealth> OnAnyEnemyDied;

    void Awake()
    {
        currentHealth = maxHealth;
        flash = GetComponent<HitFlash>() ?? GetComponent<HitFlash>();
        if (enemyAnimator == null)
            enemyAnimator = GetComponent<EnemyAnimator>();
        hitbox = GetComponent<CircleCollider2D>();
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;

        currentHealth -= amount;

        // Damage numbers
        if (DamageNumbers.Instance != null)
            DamageNumbers.Instance.Show(transform.position + Vector3.up * 0.6f, amount);
        if (flashOnLethalHit || currentHealth > 0)
            flash?.PlayFlash();

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
        hitbox.enabled = false;
        OnDeath?.Invoke();
        enemyAnimator.PlayDie();
    }

    // Called by animation event at the end of the death animation
    public void FinishDeath()
    {
        Destroy(gameObject);
    }
}
