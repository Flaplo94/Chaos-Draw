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
