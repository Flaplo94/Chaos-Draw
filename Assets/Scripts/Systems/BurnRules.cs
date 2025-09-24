using UnityEngine;

public static class BurnRules
{
    public static bool FireAddsBurn = false;
    public static float PercentOfHit = 0.25f;

    /// <summary>
    /// Kald denne efter at du har påført et Fire-hit.
    /// 'target' må være child-collider; vi finder selv roden.
    /// </summary>
    public static void TryApplyBurn(Transform target, int fireDamage)
    {
        if (!FireAddsBurn || fireDamage <= 0 || target == null) return;

        // 1) Find "roden" af fjenden (så vi ikke ender på et child GO uden EnemyHealth/BossHealth)
        Transform root = ResolveEnemyRoot(target);

        // 2) Sørg for at BurnDoT ligger på roden
        BurnDoT dot = root.GetComponent<BurnDoT>();
        if (dot == null)
            dot = root.gameObject.AddComponent<BurnDoT>();

        // 3) (Re)apply/refresh burn
        dot.ApplyNewBurn(fireDamage, PercentOfHit);
    }

    private static Transform ResolveEnemyRoot(Transform t)
    {
        if (t == null) return null;

        // attachedRigidbody = bedste bud på "root"
        if (t.TryGetComponent<Rigidbody2D>(out var rb2d) && rb2d != null)
            return rb2d.transform;

        // Hvis target er et child, så prøv at finde EnemyHealth/BossHealth i forældre
        var eh = t.GetComponentInParent<EnemyHealth>();
        if (eh != null) return eh.transform;

        var bh = t.GetComponentInParent<BossHealth>();
        if (bh != null) return bh.transform;

        // Som fallback: gå til top i hierarkiet
        Transform cur = t;
        while (cur.parent != null) cur = cur.parent;
        return cur;
    }
}
