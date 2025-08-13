using UnityEngine;
using System.Collections.Generic;

public class ChainLightning : MonoBehaviour, IAbilityBehavior
{
    [SerializeField] private float range = 5f;
    [SerializeField] private int damage = 1;
    [SerializeField] private int maxChains = 3;
    [SerializeField] private LayerMask enemyLayer;          // <- must include the Boss layer too
    [SerializeField] private GameObject castVisual;
    [SerializeField] private GameObject lightningVisual;

    public bool Initialize(Vector2 dir, Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Uncommon:
                maxChains += 1; damage += 1; break;
            case Rarity.Rare:
                maxChains += 2; damage += 2; break;
            case Rarity.Epic:
                maxChains += 3; damage += 3; break;
            case Rarity.Legendary:
                maxChains += 5; damage += 4; break;
        }
        return true;
    }

    private void Start()
    {
        if (castVisual != null)
        {
            var vfx = Instantiate(castVisual, transform.position, Quaternion.identity);
            Destroy(vfx, 0.5f);
        }

        // Make sure enemyLayer includes BOTH normal enemies AND bosses
        Collider2D[] inRange = Physics2D.OverlapCircleAll(transform.position, range, enemyLayer);

        var hitSet = new HashSet<Collider2D>();
        Vector3 prevPos = transform.position;

        for (int i = 0; i < maxChains; i++)
        {
            Collider2D nearest = null;
            float nearestDist = float.MaxValue;

            foreach (var c in inRange)
            {
                if (c == null || hitSet.Contains(c)) continue;

                float dist = Vector2.Distance(prevPos, c.transform.position);
                if (dist < nearestDist)
                {
                    nearest = c;
                    nearestDist = dist;
                }
            }

            if (nearest == null) break;

            // Draw arc
            if (lightningVisual != null)
            {
                var vfx = Instantiate(lightningVisual);
                var lr = vfx.GetComponent<LineRenderer>();
                if (lr != null)
                {
                    lr.useWorldSpace = true;
                    if (lr.positionCount < 2) lr.positionCount = 2;
                    lr.SetPosition(0, prevPos);
                    lr.SetPosition(1, nearest.transform.position);
                }
                Destroy(vfx, 0.2f);
            }

            hitSet.Add(nearest);

            // Damage enemy OR boss
            if (nearest.TryGetComponent(out EnemyHealth eh)) eh.TakeDamage(damage);
            if (nearest.TryGetComponent(out BossHealth bh)) bh.TakeDamage(damage);

            prevPos = nearest.transform.position;
        }

        Destroy(gameObject);
    }
}
