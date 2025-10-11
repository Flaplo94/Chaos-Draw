using UnityEngine;
using System.Collections.Generic;

public class EnemyFollow : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 2f;
    [SerializeField] private float separationRadius = 1.5f;
    [SerializeField] private float separationStrength = 2f;

    [Header("Behavior")]
    [SerializeField] private bool isHealer = false;

    [Header("Obstacle Avoidance")] // <-- NEW
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private float avoidDistance = 1.0f;
    [SerializeField] private float avoidStrength = 2f;

    public Vector2 FacingDir { get; private set; } = Vector2.right;

    private Transform player;
    private readonly List<EnemyFollow> allEnemies = new List<EnemyFollow>();
    private RangedEnemyAttack rangedAttack;
    private float attackRange = 0f;
    private Rigidbody2D rb;
    private bool isDead = false;

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
        if (isDead) { rb.linearVelocity = Vector2.zero; return; }

        // ADD: skip all steering while stunned so knockback is visible
        // Implement StunReceiver.IsStunned(GameObject) (or adapt this line to your StunReceiver API)
        if (StunReceiver.IsStunned(gameObject))
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

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

                    // keep facing target while idle/attacking
                    Vector2 toTargetFace = ((Vector2)target.position - rb.position);
                    if (toTargetFace.sqrMagnitude > 0.0001f)
                        FacingDir = toTargetFace.normalized;

                    return;
                }
            }
        }

        // Steering toward target
        Vector2 toTarget = ((Vector2)target.position - rb.position).normalized;

        // Separation from other enemies
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

        // obstacle avoidance
        Vector2 avoid = Vector2.zero;
        RaycastHit2D hit = Physics2D.Raycast(rb.position, toTarget, avoidDistance, obstacleMask);
        if (hit.collider != null)
        {
            Vector2 perp = Vector2.Perpendicular(hit.normal).normalized;
            if (Vector2.Dot(perp, toTarget) < 0) perp = -perp;
            avoid = perp * avoidStrength;
        }

        // Combine steering
        Vector2 finalDir = (toTarget + separation * separationStrength + avoid).normalized;

        // Drive
        rb.linearVelocity = finalDir * speed;

        // expose where we're heading so the animator can pick a set
        if (finalDir.sqrMagnitude > 0.0001f)
            FacingDir = finalDir;
    }

    public void Kill()
    {
        isDead = true;
        rb.linearVelocity = Vector2.zero;
    }

    [ContextMenu("DebugKick")]
    public void DebugKick()
    {
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            Debug.Log($"[DEBUG] Applying test impulse to {name}");
            rb.AddForce(Vector2.up * 20f, ForceMode2D.Impulse);
        }
    }
}
