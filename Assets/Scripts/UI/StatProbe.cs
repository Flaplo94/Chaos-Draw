using UnityEngine;

/// <summary>
/// Samler værdier til StatBox UI.
/// - Venstre kolonne: multipliers (1.00, 1.10, …)
/// - Højre kolonne: elementtal (vises nu som "1.40( +40%)")
/// Viktigt: Fire/Lightning/Burn påvirkes IKKE af global "atk dmg".
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

    // Global / generic damage (må gerne være påvirket af atk dmg node)
    private float GetGlobalDamageMult()
    {
        float m = 1f;
        if (playerStats) m *= Mathf.Max(0f, playerStats.damageMult);
        if (buffRef) m *= Mathf.Max(0f, buffRef.GetGenericDamageMult());
        return m;
    }

    // Element multipliers (Uden global — så “atk dmg” ikke påvirker dem)
    private float GetFireElemOnly()
    {
        float m = 1f;
        if (playerStats) m *= Mathf.Max(0f, playerStats.fireDamageMult); // hvis I bruger den
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

    // ---------------- Venstre kolonne (bruges flere steder) ----------------

    public float GetDamageMultiplier() => GetGlobalDamageMult();

    public float GetAttackSpeedMultiplier()
    {
        float m = 1f;
        if (throwingRef) m *= Mathf.Max(0.0001f, throwingRef.fireRateMult);
        if (buffRef) m *= Mathf.Max(0.0001f, buffRef.GetAttackSpeedMult());
        return m;
    }

    // Returnerer forholdet ift. baseMoveSpeed (så man ser 1.00, 1.10, …)
    public float GetMoveSpeedMultiplier()
    {
        float baseSpd = movementRef ? Mathf.Max(0.0001f, movementRef.GetBaseSpeed()) : 1f;
        float mult = buffRef ? Mathf.Max(0f, buffRef.GetMoveSpeedMult()) : 1f;
        float final = baseSpd * mult;
        return final / baseSpd;
    }

    // (Valgfrit) skud/sek baseret på cooldown (hvis nogen binder til det)
    public float GetAttacksPerSecond()
    {
        if (!throwingRef || throwingRef.baseCooldown <= 0f) return 0f;
        float rate = Mathf.Max(0.0001f, throwingRef.fireRateMult);
        return 1f / (throwingRef.baseCooldown / rate);
    }

    // Absolut movespeed (m/s) – hvis du bruger den et sted
    public float GetMoveSpeed()
    {
        float baseSpd = movementRef ? movementRef.GetBaseSpeed() : 0f;
        float mult = buffRef ? buffRef.GetMoveSpeedMult() : 1f;
        return baseSpd * mult;
    }

    // ---------------- Højre kolonne – procenter (kun element, uden global) ----------------
    public float GetFireBonusPercent() => (GetFireElemOnly() - 1f) * 100f;
    public float GetLightningBonusPercent() => (GetLightningElemOnly() - 1f) * 100f;
    public float GetBurnBonusPercent() => (GetBurnElemOnly() - 1f) * 100f;

    // ---------------- Labels “1.40( +40%)” (kun element, uden global) ----------------
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
