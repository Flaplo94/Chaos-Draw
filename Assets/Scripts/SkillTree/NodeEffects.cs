using UnityEngine;
using ChaosDraw.SkillTree;

public static class NodeEffects
{
    /// <summary>
    /// Kaldes når en node levels op (for unlock-noder også ved 1. level).
    /// newLevel = nodeens nye level efter level-up.
    /// gainedValue = den konkrete værdi (flat stat) man har opnået ved dette level-up (hvis relevant).
    /// </summary>
    public static void ApplyOnLeveled(NodeData node, int newLevel, float gainedValue)
    {
        if (!node) return;

        // 1) Flat stats håndteres via effectKey / valuePerLevel
        if (SkillTreeManager.Instance != null && SkillTreeManager.Instance.IsFlatStat(node))
        {
            // Her bruger vi node.effectKey som nøgle til hvilke bufftyper der rammes
            // og gainedValue som selve værdien (additiv til multipliers).
            ApplyFlatStat(node.effectKey, gainedValue);
            return;
        }

        // 2) Specialnoder (unlock-effekter + andre ikke-flat ting) via node.id
        //    Bruges allerede i dit projekt (fx fire_unlock, lightning_godspeedunlock, m.fl.)
        EnsurePlayerBuffManager();
        var pbm = PlayerBuffManager.Instance;
        if (!pbm)
        {
            Debug.LogWarning("[NodeEffects] PlayerBuffManager.Instance mangler.");
            return;
        }

        // Safety: kun reagér når man rent faktisk har et level (dine unlocks er one-time)
        if (newLevel <= 0)
        {
            Debug.Log("[NodeEffects] Node “" + node.id + "” har newLevel <= 0 – ingen effekt.");
            return;
        }

        switch (node.id)
        {
            // Allerede eksisterende eksempler i dit projekt:
            case "fire_unlock":
                {
                    // Global regel: Fire-spells påfører Burn-DoT (25% af endeligt Fire-hit)
                    BurnRules.FireAddsBurn = true;
                    BurnRules.PercentOfHit = 0.25f;
                    Debug.Log("[NodeEffects] Fire unlock: Fire spells now apply Burn DoT (25%).");
                    return;
                }

            case "fire_aoepulseunlock":     // Fire Rose
                {
                    if (newLevel >= 1)
                    {
                        CardUnlocks.UnlockAbilityByName("AOE Pulse");
                        Debug.Log("[NodeEffects] Unlocked card: AOE Pulse (via Fire Rose).");
                    }
                    return;
                }

            case "lightning_godspeedunlock":
            case "lightning_godspeed_unlock":
                {
                    if (newLevel >= 1)
                    {
                        CardUnlocks.UnlockAbilityByName("Godspeed");
                        Debug.Log("[NodeEffects] Unlocked card: Godspeed (via Lightning node).");
                    }
                    return;
                }

            // ------------------- NYE NODER (denne opgave) -------------------

            case "fire_25dmg":
                {
                    // +25% Fire damage = +0.25 på din fireDamageMult (additiv til multiplier der starter på 1.00)
                    pbm.AddRuntimeBonus(BuffData.BuffType.FireDamage, 0.25f);
                    pbm.SavePersistentTotals();
                    Debug.Log("[NodeEffects] Fire +25% damage anvendt (mult +0.25).");
                    return;
                }

            case "lightning_25dmg":
                {
                    // Dine systemer bruger "Thunder" internt for Lightning
                    pbm.AddRuntimeBonus(BuffData.BuffType.ThunderDamage, 0.25f);
                    pbm.SavePersistentTotals();
                    Debug.Log("[NodeEffects] Lightning +25% damage anvendt (Thunder mult +0.25).");
                    return;
                }

            // ----------------------------------------------------------------

            default:
                Debug.Log("[NodeEffects] No special handler for node '" + node.id + "'.");
                return;
        }
    }

    private static void ApplyFlatStat(string effectKey, float value)
    {
        EnsurePlayerBuffManager();
        var pbm = PlayerBuffManager.Instance;
        if (!pbm)
        {
            Debug.LogWarning("[NodeEffects] PlayerBuffManager.Instance not available even after bootstrap");
            return;
        }

        switch (effectKey)
        {
            case "flat_atk_dmg": pbm.AddRuntimeBonus(BuffData.BuffType.Damage, value); break;
            case "flat_atk_speed": pbm.AddRuntimeBonus(BuffData.BuffType.AttackSpeed, value); break;
            case "flat_movespeed": pbm.AddRuntimeBonus(BuffData.BuffType.Speed, value); break;
            case "flat_gold_gain": pbm.AddRuntimeBonus(BuffData.BuffType.GoldGain, value); break;
            case "flat_shards_gain": pbm.AddRuntimeBonus(BuffData.BuffType.ChaosShardGain, value); break;
            case "flat_lifesteal": pbm.AddRuntimeBonus(BuffData.BuffType.Lifesteal, value); break;
            case "flat_hp": pbm.AddRuntimeBonus(BuffData.BuffType.MaxHP, value); break;
            case "flat_armor": pbm.AddRuntimeBonus(BuffData.BuffType.Armor, value); break;
            case "flat_hp_regen": pbm.AddRuntimeBonus(BuffData.BuffType.HPRegen, value); break;
            // Tilføj evt. flere efter behov
            default:
                Debug.LogWarning("[NodeEffects] Unknown effectKey '" + effectKey + "'.");
                break;
        }

        pbm.SavePersistentTotals();
    }

    /// <summary> Bootstrapper PlayerBuffManager hvis den ikke findes. </summary>
    private static void EnsurePlayerBuffManager()
    {
        if (PlayerBuffManager.Instance != null) return;

        var existing = Object.FindFirstObjectByType<PlayerBuffManager>();
        if (existing != null) return;

        var go = new GameObject("PlayerBuffManager(Auto)");
        Object.DontDestroyOnLoad(go);
        go.AddComponent<PlayerBuffManager>();
    }
}
