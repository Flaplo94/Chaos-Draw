using UnityEngine;

public class FireOnGround : MonoBehaviour, IAbilityBehavior
{
    [SerializeField] private float radius = 2f;
    [SerializeField] private int damagePerTick = 1;
    [SerializeField] private float tickInterval = 0.5f;
    [SerializeField] private float duration = 3f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private GameObject fireVisual;
    [SerializeField] private Color fireColor = new Color(1f, 0.5f, 0f, 0.7f);

    private float tickTimer;
    private float lifeTimer;

    public bool Initialize(Vector2 dir, Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Uncommon: radius *= 1.2f; break;
            case Rarity.Rare: radius *= 1.4f; damagePerTick += 1; break;
            case Rarity.Epic: radius *= 1.6f; damagePerTick += 2; break;
            case Rarity.Legendary: radius *= 2f; damagePerTick += 3; break;
        }
        return true;
    }

    private void Start()
    {
        tickTimer = 0f;
        lifeTimer = duration;

        if (fireVisual != null)
        {
            GameObject vfx = Instantiate(fireVisual, transform.position, Quaternion.identity, transform);
            vfx.transform.localScale = Vector3.one * radius * 2f;
            var sr = vfx.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = fireColor;
        }
    }

    private void Update()
    {
        lifeTimer -= Time.deltaTime;
        tickTimer -= Time.deltaTime;

        if (lifeTimer <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        if (tickTimer <= 0f)
        {
            tickTimer = tickInterval;

            int tickDamage = DamageCalculator.ComputeFinalDamage(damagePerTick, DamageElement.Fire);

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius, enemyLayer);
            foreach (var hit in hits)
            {
                if (hit.TryGetComponent(out EnemyHealth eh)) eh.TakeDamage(tickDamage);
                if (hit.TryGetComponent(out BossHealth bh)) bh.TakeDamage(tickDamage);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
