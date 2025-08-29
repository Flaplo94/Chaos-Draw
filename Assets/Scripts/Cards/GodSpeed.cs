using UnityEngine;

public class GodSpeed : MonoBehaviour, IAbilityBehavior
{
    [SerializeField] private float radius = 4f;
    [SerializeField] private int damage = 1;
    [SerializeField] private float tickInterval = 0.3f;
    [SerializeField] private LayerMask enemyLayer;

    private float timer;

    public bool Initialize(Vector2 dir, Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Uncommon: radius *= 1.1f; damage += 1; break;
            case Rarity.Rare: radius *= 1.25f; damage += 2; break;
            case Rarity.Epic: radius *= 1.4f; damage += 3; break;
            case Rarity.Legendary: radius *= 1.6f; damage += 4; break;
        }
        return true;
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= tickInterval)
        {
            timer = 0f;
            DoZap();
        }
    }

    private void DoZap()
    {
        Collider2D[] hits = (enemyLayer.value == 0)
            ? Physics2D.OverlapCircleAll(transform.position, radius)
            : Physics2D.OverlapCircleAll(transform.position, radius, enemyLayer);

        foreach (var h in hits)
        {
            if (!h) continue;
            if (h.TryGetComponent(out EnemyHealth eh)) eh.TakeDamage(damage, DamageElement.Lightning);
            if (h.TryGetComponent(out BossHealth bh)) bh.TakeDamage(damage);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
#endif
}
