using UnityEngine;

public class AOEPulse : MonoBehaviour, IAbilityBehavior
{
    [Header("Tuning")]
    [SerializeField] private float radius = 3f;
    [SerializeField] private int damage = 5;
    [SerializeField] private LayerMask enemyLayer;

    public bool Initialize(Vector2 dir, Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Uncommon: radius *= 1.1f; damage += 1; break;
            case Rarity.Rare: radius *= 1.25f; damage += 2; break;
            case Rarity.Epic: radius *= 1.4f; damage += 3; break;
            case Rarity.Legendary: radius *= 1.6f; damage += 5; break;
        }
        return true;
    }

    private void Start()
    {
        Collider2D[] hits = (enemyLayer.value == 0)
            ? Physics2D.OverlapCircleAll(transform.position, radius)
            : Physics2D.OverlapCircleAll(transform.position, radius, enemyLayer);

        foreach (var h in hits)
        {
            if (!h) continue;
            if (h.TryGetComponent(out EnemyHealth eh)) eh.TakeDamage(damage, DamageElement.Fire);
            if (h.TryGetComponent(out BossHealth bh)) bh.TakeDamage(damage, DamageElement.Fire);
        }
    }
    public void OnImpactFinished() => Destroy(gameObject);

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
#endif
}
