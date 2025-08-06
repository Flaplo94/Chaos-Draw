using UnityEngine;
using System.Collections.Generic;

public class EnemyFollow : MonoBehaviour
{
    [SerializeField] private float speed = 2f;
    [SerializeField] private float separationRadius = 1.5f;
    [SerializeField] private float separationStrength = 2f;
    [SerializeField] private bool isHealer = false; // Set this true for healer enemies in the Inspector

    private Transform player;
    private static readonly List<EnemyFollow> allEnemies = new List<EnemyFollow>();
    private RangedEnemyAttack rangedAttack;
    private float attackRange = 0f;
    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        player = GameObject.FindWithTag("Player").transform;
        rangedAttack = GetComponent<RangedEnemyAttack>();
        if (rangedAttack != null)
            attackRange = rangedAttack.shootRange;
        else
            attackRange = 0.8f; // Or set a default melee range if you want
        allEnemies.Add(this);
    }

    void OnDestroy()
    {
        allEnemies.Remove(this);
    }

    void Update()
    {
        Transform target = null;

        if (isHealer)
        {
            // Find the nearest other enemy
            float minDist = float.MaxValue;
            foreach (var other in allEnemies)
            {
                if (other == this) continue;
                float dist = Vector2.Distance(transform.position, other.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    target = other.transform;
                }
            }
            if (target == null) return; // No other enemies to follow
        }
        else
        {
            target = player;
        }

        if (target == null) return;

        // Stop if in attack range (only for non-healers)
        if (!isHealer && attackRange > 0f)
        {
            float distance = Vector2.Distance(transform.position, player.position);
            if (distance <= attackRange)
                return; // Stop moving
        }

        Vector2 directionToTarget = (target.position - transform.position).normalized;
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

        Vector2 finalDirection = (directionToTarget + separation * separationStrength).normalized;
        rb.MovePosition(rb.position + finalDirection * speed * Time.deltaTime);

    }
}
