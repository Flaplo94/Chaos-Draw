using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class HolyCheeseOrbiter : MonoBehaviour
{
    [Header("Orbit")]
    public Transform owner;
    public float radius = 1.5f;
    public float degreesPerSecond = 220f;
    public float heightOffset = 0f;

    [Header("Damage")]
    public int damage = 5;

    [Header("Filtering")]
    [Tooltip("Only colliders on these layers are considered valid hurtboxes. Set this to your EnemyHurtbox layer(s).")]
    public LayerMask hitboxLayers;
    [Tooltip("If true, the collider must have an EnemyHurtbox marker component.")]
    public bool requireHitboxMarker = true;

    private CircleCollider2D circle;
    private Rigidbody2D rb;
    private float angle; // radians

    void Reset()
    {
        circle = GetComponent<CircleCollider2D>();
        circle.isTrigger = true;

        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
    }

    void Awake()
    {
        circle = GetComponent<CircleCollider2D>();
        circle.isTrigger = true;

        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
    }

    void OnEnable()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr && sr.sprite)
        {
            var b = sr.sprite.bounds;
            float r = Mathf.Max(b.extents.x, b.extents.y);
            if (r > 0.01f) circle.radius = r;
        }
    }

    void Update()
    {
        if (owner == null) { Destroy(gameObject); return; }

        angle += degreesPerSecond * Mathf.Deg2Rad * Time.deltaTime;
        Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
        transform.localPosition = offset + new Vector3(0f, heightOffset, 0f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsValidHurtbox(other)) return;

        var root = other.transform.root;

        var result = DamageCalculator.ComputeFinalDamage(damage, DamageElement.Physical);
        if (root.TryGetComponent(out EnemyHealth eh))
            eh.TakeDamage(result.amount, result.element);

        if (root.TryGetComponent(out BossHealth bh))
            bh.TakeDamage(result.amount, result.element);
    }

    bool IsValidHurtbox(Collider2D col)
    {
        // 1) Layer gate (strongest filter)
        if (hitboxLayers.value != 0 && ((1 << col.gameObject.layer) & hitboxLayers.value) == 0)
            return false;

        // 2) Optional marker check (drop EnemyHurtbox marker on real hurtboxes only)
        if (requireHitboxMarker && col.GetComponent<EnemyHitbox>() == null)
            return false;

        return true;
    }
}
