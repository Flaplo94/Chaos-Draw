using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class ChickenAllyController : MonoBehaviour
{
    [Header("Stats")]
    [Tooltip("Damage per hit (goes through DamageCalculator).")]
    public int attackDamage = 10;

    [Tooltip("Attacks per second (2 = one hit every 0.5s).")]
    public float attacksPerSecond = 1.5f;

    [Tooltip("Movement speed in units/sec.")]
    public float moveSpeed = 5f;

    [Header("Ranges")]
    [Tooltip("How far the chicken looks for targets.")]
    public float detectionRadius = 8f;

    [Tooltip("Distance at which it stops and attacks.")]
    public float attackRange = 1.1f;

    [Header("Damage")]
    public DamageElement element = DamageElement.Fire;

    [Header("Target filtering")]
    [Tooltip("Enemy hitbox layers (leave empty for all).")]
    public LayerMask hitboxLayers;

    [Tooltip("If true, only colliders with EnemyHitbox marker are valid.")]
    public bool requireHitboxMarker = false;

    [Header("Retargeting / Post-kill wait")]
    [Tooltip("AFTER killing an enemy, wait this long before looking for a new target.")]
    public float retargetInterval = 0.25f;

    [Header("Visual Facing")]
    [Tooltip("Rotate the chicken to face the target/movement direction.")]
    public bool rotateToFaceTarget = true;

    [Tooltip("Use if your sprite points up instead of right (e.g., -90).")]
    public float headingOffsetDeg = -90f;

    [Tooltip("Additionally flip the SpriteRenderer on X based on target direction.")]
    public bool alsoFlipSpriteX = false;

    [SerializeField] private SpriteRenderer spriteRenderer; // optional; auto-found if null

    // ---- runtime ----
    private Rigidbody2D rb;
    private Transform target;
    private float attackCooldown;
    private bool waitingAfterKill;
    private float waitTimer;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        if (!spriteRenderer)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    void Update()
    {
        // Post-kill idle: stand still, countdown, then look for a new target
        if (waitingAfterKill)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                waitingAfterKill = false;
                target = null; // force reacquire next frame
            }
            return; // do nothing during wait
        }

        // Acquire/validate target
        if (target == null || !TargetAlive(target))
            target = FindClosestTarget();

        if (target == null) return;

        // Move/Attack
        Vector2 toTarget = (target.position - transform.position);
        float dist = toTarget.magnitude;

        if (dist > attackRange)
        {
            // Move toward
            Vector2 dir = toTarget / Mathf.Max(0.0001f, dist);
            transform.position += (Vector3)(dir * moveSpeed * Time.deltaTime);
            FaceDirection(dir);
        }
        else
        {
            // In range: attack when cooldown elapsed
            attackCooldown -= Time.deltaTime;
            FaceDirection(toTarget.normalized);

            if (attackCooldown <= 0f)
            {
                DoAttack(target);

                float cd = Mathf.Max(0.02f, 1f / Mathf.Max(0.01f, attacksPerSecond));
                attackCooldown = cd;

                // If target died because of this hit, stop and wait retargetInterval
                if (KilledTarget(target))
                {
                    waitingAfterKill = true;
                    waitTimer = Mathf.Max(0f, retargetInterval);
                }
            }
        }
    }

    void FaceDirection(Vector2 dir)
    {
        if (rotateToFaceTarget)
        {
            float z = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + headingOffsetDeg;
            transform.rotation = Quaternion.Euler(0f, 0f, z);
        }

        if (alsoFlipSpriteX && spriteRenderer)
        {
            // Flip based on horizontal aim; tweak if your art faces left by default
            spriteRenderer.flipX = (dir.x < 0f);
        }
    }

    Transform FindClosestTarget()
    {
        int mask = (hitboxLayers.value == 0) ? Physics2D.AllLayers : hitboxLayers.value;
        var hits = Physics2D.OverlapCircleAll(transform.position, detectionRadius, mask);

        Transform bestRoot = null;
        float bestSqr = float.PositiveInfinity;

        foreach (var h in hits)
        {
            if (requireHitboxMarker && h.GetComponent<EnemyHitbox>() == null)
                continue;

            var root = h.transform.root;
            if (!TargetAlive(root)) continue;

            float sq = (root.position - transform.position).sqrMagnitude;
            if (sq < bestSqr)
            {
                bestSqr = sq;
                bestRoot = root;
            }
        }
        return bestRoot;
    }

    bool TargetAlive(Transform root)
    {
        if (!root) return false;
        if (root.TryGetComponent(out EnemyHealth eh)) return eh.GetHealth() > 0;
        if (root.TryGetComponent(out BossHealth bh)) return true; // assume boss alive until removed
        return false;
    }

    bool KilledTarget(Transform root)
    {
        if (!root) return false;
        if (root.TryGetComponent(out EnemyHealth eh)) return eh.GetHealth() <= 0;
        // We don't treat bosses as "killed" for waiting purposes
        return false;
    }

    void DoAttack(Transform root)
    {
        if (!root) return;

        var result = DamageCalculator.ComputeFinalDamage(attackDamage, element);

        if (root.TryGetComponent(out EnemyHealth eh))
            eh.TakeDamage(result.amount, result.element);

        if (root.TryGetComponent(out BossHealth bh))
            bh.TakeDamage(result.amount, result.element);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.9f, 0.1f, 0.7f);
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = new Color(1f, 0.2f, 0.1f, 0.7f);
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
#endif
}
