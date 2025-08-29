using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class NodeBinding
{
    public SkillNodeUI nodeUI;   // reference til node i scenen
    public SkillData skillData;  // hvilket SkillData asset noden repræsenterer
}

public class SkillTreeManager : MonoBehaviour
{
    public static SkillTreeManager Instance;

    [Header("Node Bindings")]
    public NodeBinding[] nodeBindings;

    // Hvilke skills er unlocked
    private Dictionary<string, bool> unlocked = new Dictionary<string, bool>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        // Init alle nodes med deres data
        foreach (var b in nodeBindings)
        {
            if (b.nodeUI != null && b.skillData != null)
                b.nodeUI.Init(b.skillData);
            else
                Debug.LogWarning("[SkillTreeManager] Node mangler data eller UI ref", this);
        }

        // Load unlocks fra PlayerPrefs
        LoadUnlocks();
    }

    public bool IsUnlocked(string id)
    {
        return unlocked.ContainsKey(id) && unlocked[id];
    }

    public void Unlock(SkillData skill)
    {
        if (skill == null) return;
        if (IsUnlocked(skill.internalID)) return;

        unlocked[skill.internalID] = true;
        Debug.Log("Unlocked: " + skill.displayName);

        // === Runtime effekter (kun hvis player/buffmanager findes) ===
        switch (skill.internalID)
        {
            case "maxhp_1":
                if (PlayerHealth.Instance != null)
                {
                    int addHP = Mathf.RoundToInt(skill.value);
                    PlayerHealth.Instance.maxHealth += addHP;
                    PlayerHealth.Instance.currentHealth += addHP;
                }
                break;

            case "move_speed_1":
                if (PlayerBuffManager.Instance != null)
                    PlayerBuffManager.Instance.AddRuntimeBonus(BuffData.BuffType.Speed, skill.value);
                break;

            case "attack_speed_1":
                if (PlayerBuffManager.Instance != null)
                    PlayerBuffManager.Instance.AddRuntimeBonus(BuffData.BuffType.AttackSpeed, skill.value);
                break;

            case "fire_damage_1":
            case "fire_damage_25":
                if (PlayerBuffManager.Instance != null)
                    PlayerBuffManager.Instance.AddRuntimeBonus(BuffData.BuffType.FireDamage, skill.value);
                break;

            case "burn_chance_1":
                if (PlayerBuffManager.Instance != null)
                    PlayerBuffManager.Instance.AddRuntimeBonus(BuffData.BuffType.BurnDamage, skill.value);
                break;

            case "fireball_double_damage":
                if (PlayerBuffManager.Instance != null)
                    PlayerBuffManager.Instance.AddRuntimeBonus(BuffData.BuffType.ScalingDamage, skill.value);
                break;

            case "gold_gain_1":
                if (PlayerBuffManager.Instance != null)
                    PlayerBuffManager.Instance.AddRuntimeBonus(BuffData.BuffType.GoldGain, skill.value);
                break;

            case "chaos_shard_gain_1":
                if (PlayerBuffManager.Instance != null)
                    PlayerBuffManager.Instance.AddRuntimeBonus(BuffData.BuffType.ChaosShardGain, skill.value);
                break;

            case "legacy_deck_unlock":
                if (MetaProgressionManager.Instance != null)
                {
                    MetaProgressionManager.Instance.legacyUnlocked = true;
                    MetaProgressionManager.Instance.Save();
                }
                break;

            case "lightning_unlock":
                Debug.Log("[SkillTreeManager] Lightning Spiral unlocked.");
                if (SkillTreeUI.Instance != null)
                    SkillTreeUI.Instance.UnlockSpiral("Lightning");
                break;

            default:
                Debug.LogWarning("[SkillTreeManager] No mapping for: " + skill.internalID);
                break;
        }

        // === Gem unlocks til PlayerPrefs ===
        SaveUnlocks();

        // === Refresh UI nodes (overlays, checkmarks, naboer) ===
        foreach (var b in nodeBindings)
        {
            if (b.nodeUI != null)
                b.nodeUI.Refresh();
        }
    }

    // --- Save/Load unlocks ---
    public void SaveUnlocks()
    {
        string ids = string.Join(",", unlocked.Keys);
        PlayerPrefs.SetString("unlockedSkills", ids);
        PlayerPrefs.Save();
        Debug.Log("[SkillTreeManager] Saved unlocked skills: " + ids);
    }

    public void LoadUnlocks()
    {
        unlocked.Clear();
        string ids = PlayerPrefs.GetString("unlockedSkills", "");
        if (!string.IsNullOrEmpty(ids))
        {
            var parts = ids.Split(',');
            foreach (var id in parts)
                unlocked[id] = true;
        }

        // Refresh UI så checkmarks og overlays matcher save
        foreach (var b in nodeBindings)
        {
            if (b.nodeUI != null)
                b.nodeUI.Refresh();
        }

        Debug.Log("[SkillTreeManager] Loaded unlocked skills: " + ids);
    }

    [ContextMenu("Reset All Skills (Testing)")]
    public void ResetAllSkills()
    {
        unlocked.Clear();
        PlayerPrefs.DeleteKey("unlockedSkills");
        PlayerPrefs.Save();

        foreach (var b in nodeBindings)
            if (b.nodeUI != null)
                b.nodeUI.Refresh();

        Debug.Log("[SkillTreeManager] All skills reset!");
    }

    public bool ArePrerequisitesMet(SkillData skill)
    {
        if (skill == null || skill.prerequisites == null || skill.prerequisites.Length == 0)
            return true;

        foreach (var req in skill.prerequisites)
        {
            if (!IsUnlocked(req.internalID))
                return false;
        }
        return true;
    }
}
