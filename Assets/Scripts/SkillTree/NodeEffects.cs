using UnityEngine;
using ChaosDraw.SkillTree;

public static class NodeEffects
{
    /// <summary>
    /// Kaldes når en node levels op. newLevel = nodeens nye level efter level-up.
    /// gainedValue = den konkrete værdi (flat stat) man har opnået ved dette level-up (hvis relevant).
    /// </summary>
    public static void ApplyOnLeveled(NodeData node, int newLevel, float gainedValue)
    {
        if (!node) return;

        // 1) Flat stats
        if (SkillTreeManager.Instance != null && SkillTreeManager.Instance.IsFlatStat(node))
        {
            ApplyFlatStat(node.effectKey, gainedValue);
            return;
        }

        // 2) Special cases (by node.id)
        if (!string.IsNullOrEmpty(node.id))
        {
            switch (node.id)
            {
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

                case "lightning_godspeedunlock":   // Lightning node: Godspeed
                case "lightning_godspeed_unlock":
                    {
                        if (newLevel >= 1)
                        {
                            CardUnlocks.UnlockAbilityByName("Godspeed");
                            Debug.Log("[NodeEffects] Unlocked card: Godspeed (via Lightning node).");
                        }
                        return;
                    }

                    // (Plads til flere special-noder)
            }
        }

        Debug.Log("[NodeEffects] No special handler for node '" + node.id + "'.");
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

            default:
                Debug.LogWarning("[NodeEffects] Unknown effectKey '" + effectKey + "'.");
                break;
        }

        pbm.SavePersistentTotals();
    }

    /// <summary>
    /// Sørger for at en PlayerBuffManager eksisterer i scenen.
    /// </summary>
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
