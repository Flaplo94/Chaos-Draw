using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 5;
    private int currentHealth;

    public float damageCooldown = 1f;
    private float lastDamageTime = -Mathf.Infinity;

    void Start()
    {
        currentHealth = maxHealth;
        UpdateUI();
    }

    public void TakeDamage(int amount)
    {
        if (Time.time - lastDamageTime < damageCooldown)
            return; // Still in cooldown

        lastDamageTime = Time.time;
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