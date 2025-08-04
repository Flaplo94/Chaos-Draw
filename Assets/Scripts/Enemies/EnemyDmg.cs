using UnityEngine;
using System.Collections.Generic;

public class EnemyDamage : MonoBehaviour
{
    [SerializeField] public int damageAmount = 1;
    [SerializeField] public float attackCooldown = 1f;
    private float lastAttackTime = -Mathf.Infinity;

    private readonly List<PlayerHealth> playersInRange = new List<PlayerHealth>();

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player entered range: " + other.name); // Add this line

            PlayerHealth player = other.GetComponent<PlayerHealth>();
            if (player != null && !playersInRange.Contains(player))
                playersInRange.Add(player);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerHealth player = other.GetComponent<PlayerHealth>();
            if (player != null && playersInRange.Contains(player))
                playersInRange.Remove(player);
        }
    }

    private void Update()
    {
        if (playersInRange.Count == 0)
            return;

        if (Time.time - lastAttackTime >= attackCooldown)
        {
            foreach (var player in new List<PlayerHealth>(playersInRange))
            {
                if (player != null)
                    player.TakeDamage(damageAmount);
            }
            lastAttackTime = Time.time;
        }
    }
}
