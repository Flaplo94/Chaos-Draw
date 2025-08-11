using UnityEngine;

public class AOEPulse : MonoBehaviour, IAbilityBehavior
{
    [SerializeField] private float radius = 3f;
    [SerializeField] private int damage = 3;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private GameObject radiusVisual;
    [SerializeField] private Color aoeColor = Color.red;

    public void Initialize(Vector2 dir, Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Uncommon:
                radius *= 1.1f; break;
            case Rarity.Rare:
                radius *= 1.3f; damage += 2; break;
            case Rarity.Epic:
                radius *= 1.5f; damage += 4; break;
            case Rarity.Legendary:
                radius *= 2f; damage += 7; break;
        }
    }

    private void Start()
    {
        Collider2D[] enemies = Physics2D.OverlapCircleAll(transform.position, radius, enemyLayer);
        foreach (var enemy in enemies)
        {
            enemy.GetComponent<EnemyHealth>()?.TakeDamage(damage);
        }

        if (radiusVisual != null)
        {
            GameObject vfx = Instantiate(radiusVisual, transform.position, Quaternion.identity);
            vfx.transform.localScale = Vector3.one * radius * 2f;
            var sr = vfx.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.color = aoeColor;
            Destroy(vfx, 0.2f);
        }

        Destroy(gameObject, 0.1f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}