using System;
using UnityEngine;

[RequireComponent(typeof(HitFlash))]
public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private bool flashOnLethalHit = true;
    [SerializeField] private int goldReward = 1; // hvor meget guld denne fjende giver

    private int currentHealth;
    private HitFlash flash;
    private EnemyAnimator enemyAnimator;
    public Action OnDeath;
    private CircleCollider2D hitbox;

    // Globalt event (for alle enemies)
    public static event Action<EnemyHealth> OnAnyEnemyDied;

    private bool isDead = false; // NEW FLAG

    void Awake()
    {
        currentHealth = maxHealth;
        flash = GetComponent<HitFlash>() ?? GetComponent<HitFlash>();
        if (enemyAnimator == null)
            enemyAnimator = GetComponent<EnemyAnimator>();
        hitbox = GetComponent<CircleCollider2D>();
    }

    // Standard entrypoint
    public void TakeDamage(int amount, DamageElement element)
    {
        if (amount <= 0) return;
        if (isDead) return; // ignore damage after death

        currentHealth -= amount;

        // === Damage Numbers ===
        if (DamageNumbers.Instance != null)
            DamageNumbers.Instance.Show(transform.position, amount, element);

        if (flashOnLethalHit || currentHealth > 0)
            flash?.PlayFlash();

        if (currentHealth <= 0)
            Die();

        Debug.Log($"EnemyHealth.TakeDamage: dmg={amount}, element={element}");
    }

    public void Heal(int amount)
    {
        if (amount <= 0) return;
        if (isDead) return; // don't heal corpses
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
    }

    public int GetHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;

    void Die()
    {
        if (isDead) return; // prevent double-death
        isDead = true;

        GetComponent<EnemyFollow>()?.Kill();
        GetComponent<FlyingEnemy>()?.Kill();

        // Disable ALL colliders so bullets no longer hit
        foreach (var col in GetComponentsInChildren<Collider2D>())
            col.enabled = false;

        // === Gold Reward ===
        if (Wallet.Instance != null)
            Wallet.Instance.Add(goldReward);

        OnDeath?.Invoke();
        OnAnyEnemyDied?.Invoke(this);

        enemyAnimator?.PlayDie();
    }


    // Called by animation event at the end of the death animation
    public void FinishDeath()
    {
        Destroy(gameObject);
    }
}
