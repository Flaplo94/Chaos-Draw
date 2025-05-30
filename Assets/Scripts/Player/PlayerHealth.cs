using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 3;
    private int currentHealth;

    public GameOverUI gameOverUI;


    public float invincibilityDuration = 1f;
    private bool isInvincible = false;

    public UnityEvent onDeath;
    public UnityEvent<int, int> onHealthChanged;

    private void Start()
    {
        currentHealth = maxHealth;
        onHealthChanged.Invoke(currentHealth, maxHealth);
    }

    public void TakeDamage(int amount)
    {
        if (isInvincible) return;

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        onHealthChanged.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(InvincibilityCoroutine());
        }
    }

    private System.Collections.IEnumerator InvincibilityCoroutine()
    {
        isInvincible = true;
        yield return new WaitForSeconds(invincibilityDuration);
        isInvincible = false;
    }

    private void Die()
    {
        Debug.Log("Player has died.");
        onDeath.Invoke();
        gameOverUI.ShowGameOver();

    }
}
