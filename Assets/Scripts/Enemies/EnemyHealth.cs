using UnityEngine;
using System;

public class EnemyHealth : MonoBehaviour
{
    public int health = 3;
    public Action OnDeath;

    public void TakeDamage(int amount)
    {
        health -= amount;

        if (health <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        OnDeath?.Invoke();

        Destroy(gameObject);
    }
}
