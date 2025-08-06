using UnityEngine;


public class PlayerHealth : MonoBehaviour
{
    private PlayerShield playerShield;
    public int maxHealth = 5;
    private int currentHealth;

    public float damageCooldown = 1f;
    private float lastDamageTime = -Mathf.Infinity;

    private UIHealthBar healthBar;

    void Awake()
    {
        playerShield = GetComponent<PlayerShield>();
    }

    [System.Obsolete]
    void Start()
    {
        currentHealth = maxHealth;

        // Find UI health bar
        healthBar = FindObjectOfType<UIHealthBar>();
        if (healthBar != null)
        {
            healthBar.SetTarget(this.transform);
            healthBar.SetValue(1f); // Fuld HP
        }

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
        if (healthBar != null)
        {
            healthBar.SetValue(currentHealth / (float)maxHealth);
        }
    }
}
