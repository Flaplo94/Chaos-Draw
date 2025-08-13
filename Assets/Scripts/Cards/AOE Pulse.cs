using System.Collections.Generic;
using UnityEngine;

public class AOEPulse : MonoBehaviour, IAbilityBehavior
{
    [SerializeField] private float radius = 3f;
    [SerializeField] private int damage = 3;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private GameObject radiusVisual;
    [SerializeField] private Color aoeColor = Color.red;

    public bool Initialize(Vector2 dir, Rarity rarity)
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
        return true;
    }

    private void Start()
    {
        // If enemyLayer is unset (0), don’t filter — collect all then filter by component.
        Collider2D[] hits = (enemyLayer.value == 0)
            ? Physics2D.OverlapCircleAll(transform.position, radius)
            : Physics2D.OverlapCircleAll(transform.position, radius, enemyLayer);

        // De-dup by root transform (handles multi-collider enemies/bosses)
        var seen = new HashSet<Transform>();

        foreach (var col in hits)
        {
            if (!col) continue;

            Transform root = col.attachedRigidbody ? col.attachedRigidbody.transform : col.transform;
            if (!seen.Add(root)) continue;                 // already processed this target
            if (root.CompareTag("Player")) continue;       // just in case

            // Damage enemies and bosses
            var eh = root.GetComponent<EnemyHealth>();
            if (eh != null) eh.TakeDamage(damage);

            var bh = root.GetComponent<BossHealth>();
            if (bh != null) bh.TakeDamage(damage);
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