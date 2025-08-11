using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 5;
    private int currentHealth;

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

    void Die()
    {
        GameOverManager gameOver = FindFirstObjectByType<GameOverManager>();
        if (gameOver != null)
        {
            gameOver.TriggerGameOver();
        }

        Destroy(gameObject);
    }

    void UpdateUI()
    {
        if (healthBar != null)
        {
            healthBar.value = (float)currentHealth / maxHealth;
        }
    }
}
