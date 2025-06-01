using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public int health = 1;

    public void TakeDamage(int dmg)
    {
        health -= dmg;

        if (health <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        WaveManager.Instance.EnemyDied();
        Destroy(gameObject);
    }
}
