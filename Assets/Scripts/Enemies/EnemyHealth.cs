using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public int maxHealth = 1;
    private int currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        // Fort�l WaveManager at en fjende er d�d
        FindAnyObjectByType<WaveManager>().EnemyDied();

        // Fjern fjenden fra spillet
        Destroy(gameObject);
    }
}
