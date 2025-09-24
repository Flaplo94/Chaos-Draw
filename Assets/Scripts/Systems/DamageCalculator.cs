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
    public static bool DEBUG_LOG = false;

    /// <summary>
    /// Standard: Global "atk dmg" gælder KUN for Physical (basic attacks).
    /// Spells (Fire/Lightning/Burn) får KUN deres element-multipliers.
    /// </summary>
    public static DamageResult ComputeFinalDamage(float baseDamage, DamageElement element)
    {
        // Physical => basic attack => inkluder global
        bool includeGlobal = (element == DamageElement.Physical);
        return ComputeInternal(baseDamage, element, includeGlobal);
    }

    /// <summary>
    /// Brug hvis du VED det er et basic attack (kan være non-physical, men skal have global med).
    /// </summary>
    public static DamageResult ComputeBasicAttackDamage(float baseDamage, DamageElement element = DamageElement.Physical)
    {
        return ComputeInternal(baseDamage, element, includeGlobal: true);
    }

    /// <summary>
    /// Brug til spells: tvinger eksklusion af global, uanset element.
    /// </summary>
    public static DamageResult ComputeSpellDamage(float baseDamage, DamageElement element)
    {
        return ComputeInternal(baseDamage, element, includeGlobal: false);
    }

    // ---------------- Internal ----------------
    private static DamageResult ComputeInternal(float baseDamage, DamageElement element, bool includeGlobal)
    {
        float mult = 1f;

        var pbm = PlayerBuffManager.Instance;
        if (pbm != null)
        {
            // Global / generic "atk dmg"?
            if (includeGlobal)
                mult *= Mathf.Max(0f, pbm.GetGenericDamageMult());

            // Element-specifik multiplier
            switch (element)
            {
                case DamageElement.Fire:
                    mult *= Mathf.Max(0f, pbm.GetFireDamageMult());
                    break;
                case DamageElement.Lightning:
                    mult *= Mathf.Max(0f, pbm.GetThunderDamageMult());
                    break;
                case DamageElement.Burn:
                    mult *= Mathf.Max(0f, pbm.GetBurnDamageMult());
                    break;
                default:
                    break; // Physical: kun global (hvis includeGlobal=true)
            }
        }

        float final = baseDamage * mult;
        int intFinal = Mathf.Max(0, Mathf.RoundToInt(final));

        if (DEBUG_LOG)
            Debug.Log($"[DMG] base={baseDamage} elem={element} includeGlobal={includeGlobal} mult={mult:F3} -> {intFinal}");

        return new DamageResult(intFinal, element);
    }
}
