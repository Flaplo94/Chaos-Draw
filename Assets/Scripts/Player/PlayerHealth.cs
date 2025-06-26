using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    private PlayerShield playerShield;
    public int maxHealth = 5;
    private int currentHealth;

    void Awake()
    {
        playerShield = GetComponent<PlayerShield>();
    }
    void Start()
    {
        currentHealth = maxHealth;
        UpdateUI();
    }

    public void TakeDamage(int amount)
    {
        

        if (playerShield != null && playerShield.TryBlockDamage())
        {
            // Shield blocked the hit, do not apply damage or trigger cooldown
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
        Debug.Log("Player died!");
        GameOverManager gameOver = FindFirstObjectByType<GameOverManager>();
        if (gameOver != null)
        {
            gameOver.TriggerGameOver();
        }

        Destroy(gameObject);
    }
    void UpdateUI()
    {
        UIHealthBar.Instance.SetValue(currentHealth / (float)maxHealth);
    }
}