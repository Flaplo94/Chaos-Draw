using UnityEngine;

/// <summary>
/// Ligger på fjendens "root" og tiker Burn-skade.
/// </summary>
public class BurnDoT : MonoBehaviour
{
    [SerializeField] private float tickInterval = 0.5f;
    [SerializeField] private float duration = 2.5f;

    private float timeLeft;
    private float nextTickIn;
    private int ticksTotal;
    private int ticksLeft;
    private int damagePerTick;

    private EnemyHealth enemy;
    private BossHealth boss;

    private void Awake()
    {
        // Cache direkte — hvis vi ligger på et child ved en fejl, prøv Parent
        enemy = GetComponent<EnemyHealth>() ?? GetComponentInParent<EnemyHealth>();
        boss = GetComponent<BossHealth>() ?? GetComponentInParent<BossHealth>();
    }

    /// <summary>
    /// Kaldes for hvert nyt (seneste) Fire-hit. Overwriter/refresh'er DoT.
    /// </summary>
    public void ApplyNewBurn(int fireHitDamage, float percentOfHit)
    {
        if (fireHitDamage <= 0 || percentOfHit <= 0f) return;

        // Reset varighed
        timeLeft = duration;
        nextTickIn = tickInterval;

        // Total burn = 25% af det ENDELIGE Fire-hit * PBM Burn mult
        int totalBurn = Mathf.RoundToInt(fireHitDamage * percentOfHit);

        float mult = 1f;
        if (PlayerBuffManager.Instance != null)
            mult = Mathf.Max(0f, PlayerBuffManager.Instance.GetBurnDamageMult());

        totalBurn = Mathf.RoundToInt(totalBurn * mult);

        // Fordel jævnt over ticks
        ticksTotal = Mathf.Max(1, Mathf.RoundToInt(duration / tickInterval));
        ticksLeft = ticksTotal;
        damagePerTick = Mathf.Max(1, totalBurn / ticksTotal);
    }

    private void Update()
    {
        if (ticksLeft <= 0) { Destroy(this); return; }

        timeLeft -= Time.deltaTime;
        nextTickIn -= Time.deltaTime;

        if (nextTickIn <= 0f)
        {
            nextTickIn += tickInterval; // undgå drift
            DoTick();
            ticksLeft--;
        }

        if (timeLeft <= 0f || ticksLeft <= 0)
            Destroy(this);
    }

    private void DoTick()
    {
        if (damagePerTick <= 0) return;

        // Kritter ikke – vi kalder bare TakeDamage som normalt med Burn-element
        if (enemy != null)
            enemy.TakeDamage(damagePerTick, DamageElement.Burn);
        else if (boss != null)
            boss.TakeDamage(damagePerTick, DamageElement.Burn);
    }
}
