using UnityEngine;

/// <summary>
/// Samler alle værdier til StatBox UI.
/// - Venstre kolonne: multipliers (1.00, 1.10, …)
/// - Højre kolonne: total element-procent inkl. global damage ((global * element - 1) * 100)
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

    // =======================
    // Helpers (multipliers)
    // =======================

    private float GetGlobalDamageMult()
    {
        float m = 1f;
        if (playerStats) m *= Mathf.Max(0f, playerStats.damageMult);          // PlayerStats global
        if (buffRef) m *= Mathf.Max(0f, buffRef.GetGenericDamageMult());  // BuffManager global (fx Glass Cannon)
        return m;
    }

    private float GetFireElemMult()
    {
        float m = 1f;
        if (playerStats) m *= Mathf.Max(0f, playerStats.fireDamageMult);      // PlayerStats fire
        if (buffRef) m *= Mathf.Max(0f, buffRef.GetFireDamageMult());     // BuffManager fire (fx Charcoal)
        return m;
    }

    private float GetLightningElemMult()
    {
        float m = 1f;
        if (buffRef) m *= Mathf.Max(0f, buffRef.GetThunderDamageMult());  // BuffData hedder "Thunder"
        return m;
    }

    private float GetBurnElemMult()
    {
        float m = 1f;
        if (buffRef) m *= Mathf.Max(0f, buffRef.GetBurnDamageMult());
        return m;
    }

    // =====================================
    // Venstre kolonne – multipliers (1.xx)
    // =====================================

    // Global/basic damage multiplier
    public float GetDamageMultiplier()
    {
        return GetGlobalDamageMult(); // 1.00, 1.10, 4.00, ...
    }

    // Attack speed multiplier (din faktiske kadence-mult. Tag buff med hvis I bruger den i gameplay)
    public float GetAttackSpeedMultiplier()
    {
        float m = 1f;
        if (throwingRef) m *= Mathf.Max(0.0001f, throwingRef.fireRateMult);
        if (buffRef) m *= Mathf.Max(0.0001f, buffRef.GetAttackSpeedMult()); // hvis I benytter denne i gameplay
        return m; // 1.00, 1.05, ...
    }

    // Movement speed multiplier (slutfart / basefart)
    public float GetMoveSpeedMultiplier()
    {
        float baseSpd = movementRef ? Mathf.Max(0.0001f, movementRef.GetBaseSpeed()) : 1f;
        float mult = buffRef ? Mathf.Max(0f, buffRef.GetMoveSpeedMult()) : 1f;
        float final = baseSpd * mult;
        return final / baseSpd; // = mult -> 1.00, 1.10, ...
    }

    // ======================================
    // Højre kolonne – total element-procent
    // (inkl. global damage så Glass Cannon m.m. tæller med)
    // ======================================

    public float GetFireBonusPercent()
    {
        return (GetGlobalDamageMult() * GetFireElemMult() - 1f) * 100f;
    }

    public float GetLightningBonusPercent()
    {
        return (GetGlobalDamageMult() * GetLightningElemMult() - 1f) * 100f;
    }

    public float GetBurnBonusPercent()
    {
        return (GetGlobalDamageMult() * GetBurnElemMult() - 1f) * 100f;
    }

    // ======================================
    // (Bevar disse for bagud-kompatibilitet hvis du har bundet dem et sted)
    // ======================================

    // Endelig “basic” fysisk hit damage (heltal) – hvis du vil vise rå tal et andet sted
    public float GetBasicDamage()
    {
        int baseFromPrefab = 1;
        if (throwingRef && throwingRef.cardPrefab &&
            throwingRef.cardPrefab.TryGetComponent<Bullet>(out var bulletPrefab))
        {
            baseFromPrefab = Mathf.Max(1, bulletPrefab.baseDamage);
        }

        float final = baseFromPrefab * GetGlobalDamageMult();
        return Mathf.Max(1f, Mathf.Round(final));
    }

    // Angreb pr. sekund (kun, hvis du stadig bruger den i UI et sted)
    public float GetAttacksPerSecond()
    {
        if (!throwingRef || throwingRef.baseCooldown <= 0f) return 0f;
        float rate = Mathf.Max(0.0001f, throwingRef.fireRateMult);
        return rate / Mathf.Max(0.0001f, throwingRef.baseCooldown);
    }

    // Absolut movespeed (m/s) – hvis du skulle få brug for den
    public float GetMoveSpeed()
    {
        float baseSpd = movementRef ? movementRef.GetBaseSpeed() : 0f;
        float mult = buffRef ? buffRef.GetMoveSpeedMult() : 1f;
        return baseSpd * mult;
    }
}
