using UnityEngine;
using System.Collections.Generic;

public class EnemyFollow : MonoBehaviour
{
    [SerializeField] private float speed = 2f;
    [SerializeField] private float separationRadius = 1.5f;
    [SerializeField] private float separationStrength = 2f;
    [SerializeField] private bool isHealer = false;

    private Transform player;
    private static readonly List<EnemyFollow> allEnemies = new List<EnemyFollow>();
    private RangedEnemyAttack rangedAttack;
    private float attackRange = 0f;
    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.linearDamping = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        player = GameObject.FindWithTag("Player")?.transform;
        rangedAttack = GetComponent<RangedEnemyAttack>();
        attackRange = (rangedAttack != null) ? rangedAttack.shootRange : 0.8f;

        allEnemies.Add(this);
    }

    void OnDestroy()
    {
        allEnemies.Remove(this);
    }

    void FixedUpdate()
    {
        // reacquire if needed
        if (player == null)
        {
            var p = GameObject.FindWithTag("Player");
            if (p != null) player = p.transform;
        }

        Transform target = null;

        if (isHealer)
        {
            float minDist = float.MaxValue;
            for (int i = 0; i < allEnemies.Count; i++)
            {
                var other = allEnemies[i];
                if (other == this) continue;
                float dist = Vector2.Distance(transform.position, other.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    target = other.transform;
                }
            }
            if (target == null) { rb.linearVelocity = Vector2.zero; return; }
        }
        else
        {
            target = player;
            if (target == null) { rb.linearVelocity = Vector2.zero; return; }

            // Stop if in attack range
            if (attackRange > 0f)
            {
                float distance = Vector2.Distance(transform.position, target.position);
                if (distance <= attackRange)
                {
                    rb.linearVelocity = Vector2.zero;
                    return;
                }
            }
        }

        // Steering + separation
        Vector2 toTarget = ((Vector2)target.position - rb.position).normalized;

        Vector2 separation = Vector2.zero;
        int count = 0;
        for (int i = 0; i < allEnemies.Count; i++)
        {
            var other = allEnemies[i];
            if (other == this) continue;

            float dist = Vector2.Distance(rb.position, other.transform.position);
            if (dist < separationRadius && dist > 0f)
            {
                Vector2 away = (rb.position - (Vector2)other.transform.position).normalized / dist;
                separation += away;
                count++;
            }
        }
        if (count > 0) separation /= count;

        Vector2 finalDir = (toTarget + separation * separationStrength).normalized;

        // Drive with velocity (physics-friendly, consistent speed)
        rb.linearVelocity = finalDir * speed;
    }
}
