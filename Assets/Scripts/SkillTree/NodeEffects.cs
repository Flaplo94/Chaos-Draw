using UnityEngine;
using ChaosDraw.SkillTree;

public static class NodeEffects
{
    /// <summary>
    /// Kaldes når en node levels op. newLevel = nodeens nye level efter level-up.
    /// gainedValue = flat stat værdi for dette level-up (hvis relevant).
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

        EnsurePlayerBuffManager();
        var pbm = PlayerBuffManager.Instance;
        if (!pbm)
        {
            Debug.LogWarning("[NodeEffects] PlayerBuffManager.Instance mangler.");
            return;
        }

        if (newLevel <= 0) return;

        switch (node.id)
        {
            // -------- Fire --------
            case "fire_unlock":
                {
                    // Brug din gamle, fungerende persist-mechanik
                    BurnRules.EnableFireBurn(0.25f);
                    pbm.AddRuntimeBonus(BuffData.BuffType.BurnDamage, 0.25f);
                    pbm.SavePersistentTotals();
                    Debug.Log("[NodeEffects] Fire unlock: Burn DoT aktiveret (25%).");
                    return;
                }

            case "fire_25dmg":
                {
                    pbm.AddRuntimeBonus(BuffData.BuffType.FireDamage, 0.25f);
                    pbm.SavePersistentTotals();
                    Debug.Log("[NodeEffects] +25% Fire damage.");
                    return;
                }

            case "fire_aoepulseunlock":
            case "fire_aoe_pulse_unlock":
            case "aoe_pulse_unlock":
                {
                    if (newLevel >= 1)
                    {
                        CardUnlocks.UnlockAbilityByName("AOE Pulse");
                        Debug.Log("[NodeEffects] Unlocked: AOE Pulse.");
                    }
                    return;
                }

            // -------- Lightning --------
            case "lightning_25dmg":
                {
                    pbm.AddRuntimeBonus(BuffData.BuffType.ThunderDamage, 0.25f);
                    pbm.SavePersistentTotals();
                    Debug.Log("[NodeEffects] +25% Lightning damage.");
                    return;
                }

            case "lightning_godspeedunlock":
            case "lightning_godspeed_unlock":
            case "godspeed_unlock":
                {
                    if (newLevel >= 1)
                    {
                        CardUnlocks.UnlockAbilityByName("Godspeed");
                        Debug.Log("[NodeEffects] Unlocked: Godspeed.");
                    }
                    return;
                }

            case "lightning_unlock": // din stun-node (Storm Pact)
            case "lightning_staticcharge":
            case "lightning_stun_unlock":
            case "stun_unlock":
            case "lightningstun":
                {
                    PlayerPrefs.SetInt("lightning_stun_unlocked", 1);
                    PlayerPrefs.Save();
                    Debug.Log("[NodeEffects] Lightning stun unlocked (flag set).");
                    return;
                }

            // -------- Reroll (din tilføjelse) --------
            case "card_reroll":
                {
                    // Persistér level, så run-UI/logic kan hente det.
                    PlayerPrefs.SetInt("card_reroll_level", Mathf.Max(0, newLevel));
                    PlayerPrefs.Save();

                    // Hvis du har en metode i MetaProgressionManager til at sætte capacity direkte,
                    // så kald den her (ellers er PlayerPrefs-nøglen nok):
                    var meta = MetaProgressionManager.Instance;
                    if (meta != null)
                    {
                        // Hvis du har noget ala: meta.SetCardRerollCapacity(newLevel); så brug den.
                        // Ellers lader vi PlayerPrefs være kilden.
                        Debug.Log("[NodeEffects] Card Reroll level = " + newLevel + " (gemt).");
                    }
                    else
                    {
                        Debug.LogWarning("[NodeEffects] MetaProgressionManager ikke fundet; Reroll-level er gemt i PlayerPrefs.");
                    }
                    return;
                }

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
            Debug.LogWarning("[NodeEffects] PlayerBuffManager.Instance not available after bootstrap");
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

    private static void EnsurePlayerBuffManager()
    {
        if (PlayerBuffManager.Instance != null) return;

        var existing = Object.FindObjectOfType<PlayerBuffManager>();
        if (existing != null) return;

        var go = new GameObject("PlayerBuffManager(Auto)");
        Object.DontDestroyOnLoad(go);
        go.AddComponent<PlayerBuffManager>();
    }
}
