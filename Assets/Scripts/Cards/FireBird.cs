using UnityEngine;
using System.Collections.Generic;

public class FireBird : MonoBehaviour, IAbilityBehavior
{
    [Header("Movement & Damage")]
    [SerializeField] private float speed = 10f;
    [SerializeField] private int damage = 10;

    [Header("Piercing")]
    [SerializeField] private int basePierces = 1;    // how many targets it can pass through at Common
    [SerializeField] private float maxLifetime = 6f; // safety cap

    [Header("Visual")]
    [SerializeField] private float baseScale = 1f;   // visual size at Common (multiplies localScale)

    private Vector2 direction;
    private int remainingPierces;
    private float lifeTimer;

    // Lucky Shot: prevent duplicate from duplicating again
    private bool luckyWasDuplicated = false;

    // Track roots so multi-collider enemies don't get double-hit
    private readonly HashSet<Transform> hitRoots = new HashSet<Transform>();

    public bool Initialize(Vector2 dir, Rarity rarity)
    {
        direction = dir.normalized;
        FaceDirection(direction);

        remainingPierces = basePierces;

        // Rarity scaling: size, damage, pierces
        float scaleMul = 1f;
        switch (rarity)
        {
            case Rarity.Uncommon: scaleMul = 1.1f; damage += 2; remainingPierces += 1; break;
            case Rarity.Rare: scaleMul = 1.2f; damage += 5; remainingPierces += 2; break;
            case Rarity.Epic: scaleMul = 1.35f; damage += 8; remainingPierces += 3; break;
            case Rarity.Legendary: scaleMul = 1.5f; damage += 12; remainingPierces += 5; break;
                // Common: baseline
        }

        transform.localScale *= baseScale * scaleMul;
        return true;
    }

    private void Start()
    {
        if (!luckyWasDuplicated)
        {
            LuckyShotSystem.OnSpellCast(this, () =>
            {
                // Side-by-side relative to flight direction (perpendicular offset)
                Vector2 dir = (direction.sqrMagnitude > 0.0001f) ? direction.normalized : (Vector2)transform.right;
                Vector2 side = new Vector2(-dir.y, dir.x).normalized;

                // Base position = where the first bird spawned
                Vector3 p2 = transform.position;

                // Use the configured LuckyShot offset as the side distance
                float dist = 0.75f; // fallback
                if (LuckyShotSystem.TryConsumeSpawnOffset(out var off))
                    dist = (off.x != 0f) ? off.x : off.magnitude;

                p2 += (Vector3)(side * dist);

                // Spawn duplicate and ensure it moves in the same direction
                var dup = Instantiate(gameObject, p2, transform.rotation);
                var comp = dup.GetComponent<FireBird>();
                if (comp != null)
                {
                    comp.luckyWasDuplicated = true; // no chaining
                    comp.direction = dir;           // <-- ensure movement
                    comp.FaceDirection(dir);        // keep visuals facing
                    // Note: remainingPierces/damage/scale are already cloned from this instance.
                }
            });
        }
    }

    private void Update()
    {
        transform.position += (Vector3)direction * speed * Time.deltaTime;

        lifeTimer += Time.deltaTime;
        if (lifeTimer >= maxLifetime)
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Enemy") && !other.CompareTag("Boss"))
            return;

        Transform root = other.attachedRigidbody ? other.attachedRigidbody.transform : other.transform;
        if (!hitRoots.Add(root)) return;

        var result = DamageCalculator.ComputeFinalDamage(damage, DamageElement.Fire);
        if (root.TryGetComponent(out EnemyHealth eh)) eh.TakeDamage(result.amount, result.element);
        if (root.TryGetComponent(out BossHealth bh)) bh.TakeDamage(result.amount, result.element);

        remainingPierces--;
        if (remainingPierces < 0) Destroy(gameObject);
    }

    private void FaceDirection(Vector2 dir)
    {
        if (dir.sqrMagnitude > 0.0001f)
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f);
        Vector3 p = transform.position;
        Vector3 fwd = transform.right * 0.5f;
        Gizmos.DrawLine(p, p + fwd);
    }
#endif
}
