using UnityEngine;
using ChaosDraw.SkillTree;

public static class NodeEffects
{
    public static void ApplyOnLeveled(NodeData node, int newLevel, float gainedValue)
    {
        if (!node) return;

        if (SkillTreeManager.Instance != null && SkillTreeManager.Instance.IsFlatStat(node))
        {
            ApplyFlatStat(node.effectKey, gainedValue);
            return;
        }

        Debug.Log($"[NodeEffects] No special handler for node '{node.id}'.");
    }

    private static void ApplyFlatStat(string effectKey, float value)
    {
        EnsurePlayerBuffManager();
        var pbm = PlayerBuffManager.Instance;
        if (!pbm) { Debug.LogWarning("[NodeEffects] PlayerBuffManager.Instance not available even after bootstrap"); return; }

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
                Debug.LogWarning($"[NodeEffects] Unknown effectKey '{effectKey}'.");
                break;
        }

        // Gem totals, så de er tilbage efter scene-/session-skift
        pbm.SavePersistentTotals();
    }

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
