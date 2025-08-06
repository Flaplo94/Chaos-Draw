using UnityEngine;
using System.Collections.Generic;

public class ChainLightning : MonoBehaviour
{
    [SerializeField] private float range = 5f;
    [SerializeField] private int damage = 1;
    [SerializeField] private int maxChains = 3;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private GameObject castVisual;
    [SerializeField] private GameObject lightningVisual;

    private void Start()
    {
        if (castVisual != null)
        {
            GameObject vfx = Instantiate(castVisual, transform.position, Quaternion.identity);
            Destroy(vfx, 0.5f);
        }

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, range, enemyLayer);
        HashSet<Collider2D> hitSet = new HashSet<Collider2D>();

        Vector3 previousPosition = transform.position;

        for (int i = 0; i < maxChains; i++)
        {
            Collider2D nearest = null;
            float nearestDist = float.MaxValue;

            foreach (var enemy in hitEnemies)
            {
                if (hitSet.Contains(enemy)) continue;

                float dist = Vector2.Distance(previousPosition, enemy.transform.position);
                if (dist < nearestDist)
                {
                    nearest = enemy;
                    nearestDist = dist;
                }
            }

            if (nearest == null) break;

            // Visual: draw lightning from previousPosition to nearest.transform.position
            if (lightningVisual != null)
            {
                GameObject vfx = Instantiate(lightningVisual);
                var lr = vfx.GetComponent<LineRenderer>();
                if (lr != null)
                {
                    lr.SetPosition(0, previousPosition);
                    lr.SetPosition(1, nearest.transform.position);
                }
                Destroy(vfx, 0.2f);
            }

            hitSet.Add(nearest);
            nearest.GetComponent<EnemyHealth>()?.TakeDamage(damage);
            previousPosition = nearest.transform.position;
        }

        Destroy(gameObject);
    }
}

