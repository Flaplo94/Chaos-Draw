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
        // Fortæl WaveManager at en fjende er død
        FindAnyObjectByType<WaveManager>().EnemyDied();

        // Fjern fjenden fra spillet
        Destroy(gameObject);
    }
}
