using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    private PlayerShield playerShield;
    public int maxHealth = 5;
    private int currentHealth;
    [SerializeField] public Slider healthBar;

    void Awake()
    {
        playerShield = GetComponent<PlayerShield>();
    }

    [System.Obsolete]
    void Start()
    {
        currentHealth = maxHealth;
        UpdateUI();
    }

    public void TakeDamage(int amount)
    {
        

        if (playerShield != null && playerShield.TryBlockDamage())
        {
            return;
        }

       
        currentHealth -= amount;
        UpdateUI();

        if (currentHealth <= 0)
        {
            Die();
        }
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
