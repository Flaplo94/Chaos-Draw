using UnityEngine;

public enum ElementType { None, Fire, Cold, Lightning, Arcane, Physical }

public class PlayerStats : MonoBehaviour
{
    [Header("Damage multipliers (local base, stack with buffs)")]
    public float damageMult = 1f;          // global (local base)
    public float fireDamageMult = 1f;      // local Fire base
    public float lightningDamageMult = 1f; // NEW: local Lightning base

    /// <summary>
    /// Returns the total damage multiplier for the given element.
    /// This combines local inspector multipliers (above) with runtime buffs from PlayerBuffManager.
    /// </summary>
    public float GetDamageMult(ElementType element)
    {
        float m = 1f;

        // 1) Global/local baseline
        m *= damageMult;

        // 2) Global runtime buffs (generic/scaling) from PlayerBuffManager
        if (PlayerBuffManager.Instance != null)
        {
            m *= PlayerBuffManager.Instance.GetGenericDamageMult();
            // NOTE: if you later introduce "scaling" behavior here, multiply it in as well.
        }

        // 3) Element-specific (local + runtime)
        switch (element)
        {
            case ElementType.Fire:
                m *= fireDamageMult;
                if (PlayerBuffManager.Instance != null)
                    m *= PlayerBuffManager.Instance.GetFireDamageMult();
                break;

            case ElementType.Lightning:
                m *= lightningDamageMult;
                if (PlayerBuffManager.Instance != null)
                    m *= PlayerBuffManager.Instance.GetThunderDamageMult(); // BuffData uses "Thunder"
                break;

            // You can extend these later if you add Cold/Arcane/Physical element-specific buffs
            default:
                break;
        }

        return m;
    }
}
