using UnityEngine;
using System.Collections.Generic;

public class FireBird : MonoBehaviour, IAbilityBehavior
{
    [Header("Movement & Damage")]
    [SerializeField] private float speed = 10f;
    [SerializeField] private int damage = 10;

    [Header("Piercing")]
    [SerializeField] private int basePierces = 1;
    [SerializeField] private float maxLifetime = 6f;

    [Header("Visual")]
    [SerializeField] private float baseScale = 1f;

    private Vector2 direction;
    private int remainingPierces;
    private float lifeTimer;
    private readonly HashSet<Transform> hitRoots = new HashSet<Transform>();

    public bool Initialize(Vector2 dir, Rarity rarity)
    {
        direction = dir.normalized;
        remainingPierces = basePierces;

        float scaleMul = 1f;
        switch (rarity)
        {
            case Rarity.Uncommon: scaleMul = 1.1f; damage += 2; remainingPierces += 1; break;
            case Rarity.Rare: scaleMul = 1.2f; damage += 5; remainingPierces += 2; break;
            case Rarity.Epic: scaleMul = 1.35f; damage += 8; remainingPierces += 3; break;
            case Rarity.Legendary: scaleMul = 1.5f; damage += 12; remainingPierces += 5; break;
        }

        transform.localScale *= baseScale * scaleMul;
        return true;
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
        if (!other.CompareTag("Enemy") && !other.CompareTag("Boss"))
            return;

        Transform root = other.attachedRigidbody ? other.attachedRigidbody.transform : other.transform;
        if (hitRoots.Contains(root)) return;
        hitRoots.Add(root);

        int finalDamage = DamageCalculator.ComputeFinalDamage(damage, DamageElement.Fire);

        if (root.TryGetComponent(out EnemyHealth eh)) eh.TakeDamage(finalDamage);
        if (root.TryGetComponent(out BossHealth bh)) bh.TakeDamage(finalDamage);

        remainingPierces--;
        if (remainingPierces < 0)
            Destroy(gameObject);
    }
}
