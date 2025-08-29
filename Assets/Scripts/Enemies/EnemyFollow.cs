using UnityEngine;
using System.Collections.Generic;

public class EnemyFollow : MonoBehaviour
{
    [SerializeField] private float speed = 2f;
    [SerializeField] private float separationRadius = 1.5f;
    [SerializeField] private float separationStrength = 2f;
    [SerializeField] private bool isHealer = false;

    public Vector2 FacingDir { get; private set; } = Vector2.right; // <-- NEW

    private Transform player;
    private readonly List<EnemyFollow> allEnemies = new List<EnemyFollow>();
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

    void OnDestroy() => allEnemies.Remove(this);

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

            // Stop if in attack range, but keep facing the player
            if (attackRange > 0f)
            {
                float distance = Vector2.Distance(transform.position, target.position);
                if (distance <= attackRange)
                {
                    rb.linearVelocity = Vector2.zero;

                    // <-- NEW: keep facing target while idle/attacking
                    Vector2 toTargetFace = ((Vector2)target.position - rb.position);
                    if (toTargetFace.sqrMagnitude > 0.0001f)
                        FacingDir = toTargetFace.normalized;

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

        // Drive
        rb.linearVelocity = finalDir * speed;

        // <-- NEW: expose where we're heading so the animator can pick a set
        if (finalDir.sqrMagnitude > 0.0001f)
            FacingDir = finalDir;
    }
}
