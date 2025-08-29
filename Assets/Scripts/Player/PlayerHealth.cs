using UnityEngine;
using UnityEngine.UI;
using System;


public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 5;
    private int currentHealth;
    public Action OnDeath;


    [SerializeField] public Slider healthBar;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    void Start()
    {
        UpdateUI();
    }

    public void TakeDamage(int amount)
    {
        // NEW: let an active Shield consume the hit BEFORE applying damage
        if (ShieldActiveAndConsumed())
        {
            // Shield blocked this hit; do not reduce player HP
            return;
        }

        currentHealth -= amount;
        UpdateUI();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private bool ShieldActiveAndConsumed()
    {
        // Check if a Shield instance is active and consume one shield hit
        // (Shield class manages its own internal HP and destroy logic)
        if (Shield.Active != null)
        {
            bool consumed = Shield.Active.ConsumeHit();
            if (consumed)
                return true;
        }
        return false;
    }

    public void Die()
    {
        // Notify listeners (e.g. PlayerAnimator)
        OnDeath?.Invoke();

        // Disable input/movement so the player can't keep moving
        var movement = GetComponent<PlayerMovement>();
        if (movement != null)
            movement.enabled = false;

        // Disable collider & rigidbody so enemies don't keep hitting corpse
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        // Trigger Game Over after short delay (let death anim play)
        GameOverManager gameOver = FindFirstObjectByType<GameOverManager>();
        if (gameOver != null)
        {
            // delay game over slightly so player sees the animation
            gameOver.TriggerGameOver();
        }

        // Optionally destroy object after 1–2 seconds if needed
        Destroy(gameObject, 2f);
    }


    void UpdateUI()
    {
        if (healthBar != null)
        {
            healthBar.value = (float)currentHealth / maxHealth;
        }
    }
}
