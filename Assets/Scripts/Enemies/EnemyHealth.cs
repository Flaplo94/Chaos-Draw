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

    private bool isDead = false;

    void Awake()
    {
        currentHealth = maxHealth;
        flash = GetComponent<HitFlash>() ?? GetComponent<HitFlash>();
        if (enemyAnimator == null)
            enemyAnimator = GetComponent<EnemyAnimator>();
        hitbox = GetComponent<CircleCollider2D>();
    }

    // === Standard damage entrypoint (bruges fortsat af alt eksisterende) ===
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
    }

    // === Basic attacks med lifesteal ===
    public void TakeBasicAttackDamage(int amount, DamageElement element)
    {
        if (amount <= 0) return;
        if (isDead) return;

        // Reuse standard apply logic (inkl. numbers/flash/death)
        TakeDamage(amount, element);

        // Lifesteal fra basic attacks (heal spiller med damage * lifestealMult)
        var pbm = PlayerBuffManager.Instance;
        float ls = pbm ? pbm.GetLifestealMult() : 0f; // 0.10 = 10% lifesteal
        if (ls > 0f && PlayerHealth.Instance != null)
        {
            int heal = Mathf.RoundToInt(amount * ls);
            if (heal > 0)
                PlayerHealth.Instance.Heal(heal);
        }
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
        if (isDead) return;
        isDead = true;

        GetComponent<EnemyFollow>()?.Kill();
        GetComponent<FlyingEnemy>()?.Kill();

        // Disable ALL colliders so bullets no longer hit
        foreach (var col in GetComponentsInChildren<Collider2D>())
            col.enabled = false;

        // === Gold Reward ===
        if (Wallet.Instance != null)
            Wallet.Instance.Add(goldReward);

        OnAnyEnemyDied?.Invoke(this);

        enemyAnimator?.PlayDie();
    }

    // Called by animation event at the end of the death animation
    public void FinishDeath()
    {
        OnDeath?.Invoke();
        Destroy(gameObject);
    }
}
