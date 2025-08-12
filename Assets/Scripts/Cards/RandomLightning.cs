using UnityEngine;
using System.Collections.Generic;

public class RandomLightning : MonoBehaviour, IAbilityBehavior
{
    [SerializeField] private float radius = 6f;
    [SerializeField] private int minStrikes = 1;
    [SerializeField] private int maxStrikes = 2;
    [SerializeField] private int damage = 2;
    [SerializeField] private GameObject lightningEffect;

    // rarity scaling
    private int bonusStrikes = 0;
    private float damageMultiplier = 1f;

    public void Initialize(Vector2 _, Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Uncommon: bonusStrikes = 1; damageMultiplier = 1.10f; break;
            case Rarity.Rare: bonusStrikes = 2; damageMultiplier = 1.20f; break;
            case Rarity.Epic: bonusStrikes = 3; damageMultiplier = 1.30f; break;
            case Rarity.Legendary: bonusStrikes = 4; damageMultiplier = 1.40f; break;
                // Common = baseline
        }
    }

    private void Start()
    {
        // Collect valid targets by component (works for both enemies and bosses)
        Collider2D[] inRange = Physics2D.OverlapCircleAll(transform.position, radius);
        List<Collider2D> valid = new List<Collider2D>(inRange.Length);

        for (int i = 0; i < inRange.Length; i++)
        {
            var c = inRange[i];
            if (c == null) continue;

            // Must have either EnemyHealth or BossHealth
            if (c.GetComponent<EnemyHealth>() != null || c.GetComponent<BossHealth>() != null)
            {
                valid.Add(c);
            }
        }

        if (valid.Count == 0) { Destroy(gameObject); return; }

        // Apply rarity bonuses
        int adjMin = Mathf.Max(0, minStrikes + bonusStrikes);
        int adjMax = Mathf.Max(adjMin, maxStrikes + bonusStrikes);
        int strikeCount = Mathf.Clamp(Random.Range(adjMin, adjMax + 1), 0, valid.Count);
        int finalDamage = Mathf.Max(1, Mathf.RoundToInt(damage * damageMultiplier));

        // Strike unique random targets
        for (int i = 0; i < strikeCount; i++)
        {
            int idx = Random.Range(0, valid.Count);
            var targetCol = valid[idx];
            valid.RemoveAt(idx);

            if (targetCol == null) continue;
            var t = targetCol.transform;

            // Deal damage to either type
            var eh = targetCol.GetComponent<EnemyHealth>();
            if (eh != null) eh.TakeDamage(finalDamage);

            var bh = targetCol.GetComponent<BossHealth>();
            if (bh != null) bh.TakeDamage(finalDamage);

            // Optional VFX
            if (lightningEffect != null)
            {
                var vfx = Instantiate(lightningEffect);

                
                if (vfx.TryGetComponent<LineRenderer>(out var lr))
                {
                    lr.useWorldSpace = true;
                    if (lr.positionCount < 2) lr.positionCount = 2;
                    lr.SetPosition(0, transform.position);
                    lr.SetPosition(1, t.position);
                    Destroy(vfx, 0.12f);
                }
                else
                {
                    // Otherwise spawn a burst at the target
                    vfx.transform.position = t.position;
                    Destroy(vfx, 0.25f);
                }
            }
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.6f, 0.9f, 1f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
