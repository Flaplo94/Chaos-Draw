using UnityEngine;

/// <summary>
/// Samler værdier til StatBox UI.
/// - Venstre kolonne: multipliers (1.00, 1.10, ...)
/// - Højre kolonne: elementtal (vises nu som "1.40( +40%)")
/// Vigtigt: Fire/Lightning/Burn påvirkes IKKE af global "atk dmg".
/// </summary>
public class StatProbe : MonoBehaviour
{
    [Header("Refs (auto-fyldes hvis tomme)")]
    [SerializeField] private PlayerThrowing throwingRef;
    [SerializeField] private PlayerMovement movementRef;
    [SerializeField] private PlayerBuffManager buffRef;
    [SerializeField] private PlayerStats playerStats;

    void Awake()
    {
        if (!throwingRef) throwingRef = FindFirstObjectByType<PlayerThrowing>();
        if (!movementRef) movementRef = FindFirstObjectByType<PlayerMovement>();
        if (!buffRef) buffRef = PlayerBuffManager.Instance ?? FindFirstObjectByType<PlayerBuffManager>();
        if (!playerStats) playerStats = FindFirstObjectByType<PlayerStats>();
    }

    // ---------------- Helpers (multipliers) ----------------

    private float GetGlobalDamageMult()
    {
        float m = 1f;
        if (playerStats) m *= Mathf.Max(0f, playerStats.damageMult);
        if (buffRef) m *= Mathf.Max(0f, buffRef.GetGenericDamageMult());
        return m;
    }

    private float GetFireElemOnly()
    {
        float m = 1f;
        if (playerStats) m *= Mathf.Max(0f, playerStats.fireDamageMult);
        if (buffRef) m *= Mathf.Max(0f, buffRef.GetFireDamageMult());
        return m;
    }
    private float GetLightningElemOnly()
    {
        float m = 1f;
        if (buffRef) m *= Mathf.Max(0f, buffRef.GetThunderDamageMult());
        return m;
    }
    private float GetBurnElemOnly()
    {
        float m = 1f;
        if (buffRef) m *= Mathf.Max(0f, buffRef.GetBurnDamageMult());
        return m;
    }

    // ---------------- Offentlige getters til binders ----------------

    public float GetDamageMultiplier() => GetGlobalDamageMult();
    public float GetAttackSpeedMultiplier() => buffRef ? Mathf.Max(0f, buffRef.GetAttackSpeedMult()) : 1f;
    public float GetMoveSpeedMultiplier() => GetMoveSpeed() / Mathf.Max(0.0001f, movementRef ? movementRef.GetBaseSpeed() : 1f);
    public float GetAttacksPerSecond()
    {
        if (!throwingRef || throwingRef.baseCooldown <= 0f) return 0f;
        float rate = Mathf.Max(0.0001f, throwingRef.fireRateMult);
        return 1f / (throwingRef.baseCooldown / rate);
    }
    public float GetMoveSpeed()
    {
        float baseSpd = movementRef ? movementRef.GetBaseSpeed() : 1f;
        float mult = buffRef ? Mathf.Max(0f, buffRef.GetMoveSpeedMult()) : 1f;
        return baseSpd * mult;
    }

    // Bonus-procenter (kun element, uden global)
    public float GetFireBonusPercent() => (GetFireElemOnly() - 1f) * 100f;
    public float GetLightningBonusPercent() => (GetLightningElemOnly() - 1f) * 100f;
    public float GetBurnBonusPercent() => (GetBurnElemOnly() - 1f) * 100f;

    // NY: Burn Apply % (25 hvis noden er aktiv, ellers 0)
    public float GetBurnApplyPercent()
    {
        return (BurnRules.FireAddsBurn ? BurnRules.PercentOfHit * 100f : 0f);
    }

    // Labels "1.40( +40%)" (kun element, uden global)
    public string GetFireLabel()
    {
        float mult = GetFireElemOnly();
        float pct = (mult - 1f) * 100f;
        return $"{mult:0.##}( +{pct:0.#}%)";
    }
    public string GetLightningLabel()
    {
        float mult = GetLightningElemOnly();
        float pct = (mult - 1f) * 100f;
        return $"{mult:0.##}( +{pct:0.#}%)";
    }
    public string GetBurnLabel()
    {
        float mult = GetBurnElemOnly();
        float pct = (mult - 1f) * 100f;
        return $"{mult:0.##}( +{pct:0.#}%)";
    }
}
