using UnityEngine;

public static class BurnRules
{
    private const string K_FIRE_APPLIES_BURN = "ChaosDraw_FireAppliesBurn";
    private const string K_BURN_PERCENT = "ChaosDraw_BurnPercentOfHit";

    public static bool FireAddsBurn { get; private set; } = false;
    public static float PercentOfHit { get; private set; } = 0.25f;

    // Auto-load ved første brug efter domain reload
    static BurnRules()
    {
        FireAddsBurn = PlayerPrefs.GetInt(K_FIRE_APPLIES_BURN, 0) == 1;
        PercentOfHit = PlayerPrefs.GetFloat(K_BURN_PERCENT, 0.25f);
    }

    public static void EnableFireBurn(float percentOfHit)
    {
        FireAddsBurn = true;
        PercentOfHit = Mathf.Max(0f, percentOfHit);

        PlayerPrefs.SetInt(K_FIRE_APPLIES_BURN, FireAddsBurn ? 1 : 0);
        PlayerPrefs.SetFloat(K_BURN_PERCENT, PercentOfHit);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Kald denne efter et endeligt Fire-hit. 'target' kan være child—vi finder roden.
    /// </summary>
    public static void TryApplyBurn(Transform target, int fireDamage)
    {
        if (!FireAddsBurn || fireDamage <= 0 || target == null) return;

        Transform root = ResolveEnemyRoot(target);
        if (root == null) return;

        BurnDoT dot = root.GetComponent<BurnDoT>();
        if (dot == null) dot = root.gameObject.AddComponent<BurnDoT>();

        dot.ApplyNewBurn(fireDamage, PercentOfHit);
    }

    private static Transform ResolveEnemyRoot(Transform t)
    {
        if (t == null) return null;

        if (t.TryGetComponent<Rigidbody2D>(out var rb2d) && rb2d != null)
            return rb2d.transform;

        var eh = t.GetComponentInParent<EnemyHealth>();
        if (eh != null) return eh.transform;

        var bh = t.GetComponentInParent<BossHealth>();
        if (bh != null) return bh.transform;

        // fallback: top of hierarchy
        Transform cur = t;
        while (cur.parent != null) cur = cur.parent;
        return cur;
    }
}
