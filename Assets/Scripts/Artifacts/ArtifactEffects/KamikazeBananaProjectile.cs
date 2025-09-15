using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class KamikazeBananaProjectile : MonoBehaviour
{
    [Header("Targeting")]
    public Transform target;
    public float speed = 12f;
    public float turnDegreesPerSecond = 720f;      // how fast we turn toward target
    public bool alignToTarget = true;              // face movement direction?
    public float headingOffsetDeg = 0f;            // use if your sprite points up instead of right

    [Header("Spin (visual only)")]
    public Transform visual;                       // assign the "Visual" child (with SpriteRenderer)
    public float spinDegreesPerSecond = 720f;      // set to 0 to disable spin

    [Header("Damage")]
    public int bossDamage = 50;
    public DamageElement element = DamageElement.Fire;

    [Header("Collisions")]
    public LayerMask hitboxLayers;
    public bool requireHitboxMarker = false;

    [Header("Lifetime")]
    public float maxLifetime = 8f;

    Rigidbody2D rb;
    float life;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        var col = GetComponent<Collider2D>();
        col.isTrigger = true;

        if (visual == null) visual = transform; // fallback if you kept the SpriteRenderer on root
    }

    void Update()
    {
        life += Time.deltaTime;
        if (life >= maxLifetime || target == null)
        {
            Destroy(gameObject);
            return;
        }

        // Homing direction
        Vector2 dir = ((Vector2)target.position - (Vector2)transform.position).normalized;

        // Move forward
        transform.position += (Vector3)(dir * speed * Time.deltaTime);

        // Face direction (optional)
        if (alignToTarget)
        {
            float desiredZ = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + headingOffsetDeg;
            float newZ = Mathf.MoveTowardsAngle(transform.eulerAngles.z, desiredZ, turnDegreesPerSecond * Time.deltaTime);
            transform.rotation = Quaternion.Euler(0f, 0f, newZ);
        }

        // Spin visual (purely aesthetic; collider/aim unaffected)
        if (spinDegreesPerSecond != 0f && visual != null)
            visual.Rotate(0f, 0f, spinDegreesPerSecond * Time.deltaTime, Space.Self);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsValidHitbox(other)) return;

        var root = other.transform.root;

        // Boss: fixed damage
        if (root.TryGetComponent(out BossHealth bh))
        {
            var result = DamageCalculator.ComputeFinalDamage(bossDamage, element);
            bh.TakeDamage(result.amount, result.element);
            Destroy(gameObject);
            return;
        }

        // Normal enemy: kill regardless of HP
        if (root.TryGetComponent(out EnemyHealth eh))
        {
            int hp = eh.GetHealth();
            var result = DamageCalculator.ComputeFinalDamage(hp, element);
            eh.TakeDamage(result.amount, result.element);
            Destroy(gameObject);
        }
    }

    bool IsValidHitbox(Collider2D col)
    {
        if (hitboxLayers.value != 0 && ((1 << col.gameObject.layer) & hitboxLayers.value) == 0)
            return false;

        if (requireHitboxMarker && col.GetComponent<EnemyHitbox>() == null)
            return false;

        return true;
    }
}
