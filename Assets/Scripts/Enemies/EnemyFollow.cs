using UnityEngine;
using System.Collections.Generic;

public class EnemyFollow : MonoBehaviour
{
    [SerializeField] private float speed = 2f;
    [SerializeField] private float separationRadius = 1.5f;
    [SerializeField] private float separationStrength = 2f;

    private Transform player;

    private static readonly List<EnemyFollow> allEnemies = new List<EnemyFollow>();

    void Start()
    {
        player = GameObject.FindWithTag("Player").transform;
        allEnemies.Add(this);
    }

    void OnDestroy()
    {
        allEnemies.Remove(this);

        if (PlayerXP.Instance != null)
        {
            PlayerXP.Instance.GainXP(1);
        }
    }

    void Update()
    {
        if (player == null) return;

        Vector2 directionToPlayer = (player.position - transform.position).normalized;
        Vector2 separation = Vector2.zero;
        int count = 0;

        foreach (var other in allEnemies)
        {
            if (other == this) continue;

            float dist = Vector2.Distance(transform.position, other.transform.position);
            if (dist < separationRadius && dist > 0f)
            {
                Vector2 push = (Vector2)(transform.position - other.transform.position);
                separation += push.normalized / dist;
                count++;
            }
        }

        if (count > 0)
            separation /= count;

        Vector2 finalDirection = (directionToPlayer + separation * separationStrength).normalized;
        transform.position += (Vector3)(finalDirection * speed * Time.deltaTime);
    }
}
