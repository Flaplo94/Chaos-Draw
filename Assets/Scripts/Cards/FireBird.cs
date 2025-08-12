using UnityEngine;
using System.Collections.Generic;

public class FireBird : MonoBehaviour, IAbilityBehavior
{
    [Header("Movement & Damage")]
    [SerializeField] private float speed = 10f;
    [SerializeField] private int damage = 10;

    [Header("Piercing")]
    [SerializeField] private int basePierces = 1;       // how many enemies it can pass through at Common
    [SerializeField] private float maxLifetime = 6f;    // safety so it can’t fly forever

    [Header("Visual")]
    [SerializeField] private float baseScale = 1f;      // visual size at Common (multiplies localScale)

    private Vector2 direction;
    private int remainingPierces;
    private float lifeTimer;
    private readonly HashSet<Transform> hitRoots = new HashSet<Transform>(); // avoid multi-hit on same target

    public void Initialize(Vector2 dir, Rarity rarity)
    {
        direction = dir.normalized;
        remainingPierces = basePierces;

        // Rarity scaling: bigger, stronger, pierces more
        float scaleMul = 1f;
        switch (rarity)
        {
            case Rarity.Uncommon:
                scaleMul = 1.1f; damage += 2; remainingPierces += 1; break;
            case Rarity.Rare:
                scaleMul = 1.2f; damage += 5; remainingPierces += 2; break;
            case Rarity.Epic:
                scaleMul = 1.35f; damage += 8; remainingPierces += 3; break;
            case Rarity.Legendary:
                scaleMul = 1.5f; damage += 12; remainingPierces += 5; break;
                // Common: baseline
        }

        // Apply visual size
        transform.localScale *= baseScale * scaleMul;
    }

    void Update()
    {
        transform.position += (Vector3)direction * speed * Time.deltaTime;

        lifeTimer += Time.deltaTime;
        if (lifeTimer >= maxLifetime)
            Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Match Fireball’s style: only react to enemies/bosses
        if (!other.CompareTag("Enemy") && !other.CompareTag("Boss"))
            return;

        // Use root to avoid multi-hit from child colliders
        Transform root = other.attachedRigidbody ? other.attachedRigidbody.transform : other.transform;
        if (hitRoots.Contains(root))
            return; // already damaged this target once
        hitRoots.Add(root);

        // Deal damage
        if (root.TryGetComponent(out EnemyHealth eh))
            eh.TakeDamage(damage);
        if (root.TryGetComponent(out BossHealth bh))
            bh.TakeDamage(damage);

        // Consume a pierce and continue flying until we run out
        remainingPierces--;
        if (remainingPierces < 0)
            Destroy(gameObject);
    }
}
