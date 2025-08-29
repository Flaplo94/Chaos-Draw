using UnityEngine;

public enum DamageElement
{
    Physical,
    Fire,
    Lightning,
    Burn
}

public struct DamageResult
{
    public int amount;
    public DamageElement element;

    public DamageResult(int amount, DamageElement element)
    {
        this.amount = amount;
        this.element = element;
    }
}

public static class DamageCalculator
{
    public static bool DEBUG_LOG = true; // slå fra når du er færdig

    /// <summary>
    /// Udregner final damage og returnerer både tal og element-type
    /// </summary>
    public static DamageResult ComputeFinalDamage(float baseDamage, DamageElement element)
    {
        float mult = 1f;

        if (PlayerBuffManager.Instance != null)
        {
            // Generic (inkl. ScalingDamage)
            mult *= PlayerBuffManager.Instance.GetGenericDamageMult();

            // Element-specific
            switch (element)
            {
                case DamageElement.Fire:
                    mult *= PlayerBuffManager.Instance.GetFireDamageMult();
                    break;
                case DamageElement.Lightning:
                    mult *= PlayerBuffManager.Instance.GetThunderDamageMult(); // OBS: dit BuffData bruger "Thunder"
                    break;
                case DamageElement.Burn:
                    mult *= PlayerBuffManager.Instance.GetBurnDamageMult();
                    break;
                default:
                    break; // neutral -> kun generic
            }
        }

        float final = baseDamage * mult;
        int intFinal = Mathf.Max(0, Mathf.RoundToInt(final));

        if (DEBUG_LOG)
            Debug.Log($"[DMG] base={baseDamage} elem={element} mult={mult:F3} -> {intFinal}");

        return new DamageResult(intFinal, element);
    }
}
