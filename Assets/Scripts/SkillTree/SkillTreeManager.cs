using System.Collections.Generic;
using UnityEngine;
using ChaosDraw.SkillTree;

[System.Serializable]
public class NodeBinding
{
    public SkillTreeNode nodeUI;
    public NodeData nodeData;
}

public class SkillTreeManager : MonoBehaviour
{
    public static SkillTreeManager Instance;

    [Header("Node Bindings")]
    [SerializeField] public NodeBinding[] nodeBindings;

    [Header("UI Detail Panel")]
    [SerializeField] private NodeDetailPanel detailPanel;

    // Legacy bool + nye levels
    private readonly Dictionary<string, bool> unlocked = new Dictionary<string, bool>();
    private readonly Dictionary<string, int> nodeLevels = new Dictionary<string, int>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        // Bind visuals
        foreach (var b in nodeBindings)
        {
            if (b.nodeUI != null && b.nodeData != null)
                b.nodeUI.Bind(b.nodeData);
            else
                Debug.LogWarning("[SkillTreeManager] Node mangler data eller UI ref", this);
        }

        LoadUnlocksLegacy();
        LoadLevels();

        // sync legacy bool -> level 1
        foreach (var kv in unlocked)
            if (kv.Value && GetLevel(kv.Key) == 0)
                nodeLevels[kv.Key] = 1;

        SaveLevels();
        RefreshAllNodeStates();

        if (detailPanel != null && detailPanel.gameObject.activeSelf)
            detailPanel.gameObject.SetActive(false);
    }

    // -------- Helpers: levels/valuta --------
    public int GetLevel(string id) => nodeLevels.TryGetValue(id, out var lv) ? lv : 0;
    public bool IsUnlocked(string id) => GetLevel(id) > 0;
    public bool IsMaxed(NodeData n) => n && GetLevel(n.id) >= Mathf.Max(1, n.maxLevel);

    // Flad stat? => bruger XP; ellers Shards
    public bool IsFlatStat(NodeData n)
    {
        if (n == null) return false;
        if (n.maxLevel > 1) return true;
        return n.valuePerLevel != null && n.valuePerLevel.Count > 0;
    }

    public int GetNextCost(NodeData n)
    {
        if (!n) return 0;
        int cur = GetLevel(n.id);
        int idx = cur;
        if (n.costPerLevel != null && n.costPerLevel.Count > idx)
            return Mathf.Max(0, n.costPerLevel[idx]);
        return Mathf.Max(0, n.cost);
    }

    public float GetNextValue(NodeData n)
    {
        if (!n) return 0f;
        int cur = GetLevel(n.id);
        int idx = cur;
        if (n.valuePerLevel != null && n.valuePerLevel.Count > idx)
            return n.valuePerLevel[idx];
        return 0f;
    }

    // -------- Prereqs (med minPrerequisites) --------
    public bool ArePrerequisitesMet(NodeData node)
    {
        if (node == null || node.prerequisites == null || node.prerequisites.Count == 0)
            return true;

        int required = (node.minPrerequisites > 0)
            ? Mathf.Clamp(node.minPrerequisites, 1, node.prerequisites.Count)
            : node.prerequisites.Count;

        int ok = 0;
        foreach (var req in node.prerequisites)
        {
            if (req == null) continue;
            if (IsUnlocked(req.id))
            {
                ok++;
                if (ok >= required) return true;
            }
        }
        return false;
    }

    // -------- Unlock/Upgrade --------
    public void Unlock(NodeData node) => UnlockOrUpgrade(node);

    public bool UnlockOrUpgrade(NodeData node)
    {
        if (!node) return false;

        int cur = GetLevel(node.id);
        if (cur == 0 && !ArePrerequisitesMet(node)) return false;
        if (cur >= Mathf.Max(1, node.maxLevel)) return false; // maxed

        int price = GetNextCost(node);
        var meta = MetaProgressionManager.Instance;
        if (!meta) { Debug.LogWarning("[SkillTree] Meta system missing"); return false; }

        bool paid = false;
        if (IsFlatStat(node))
            paid = meta.SpendXP(price);
        else
            paid = meta.SpendShards(price);

        if (!paid) return false;

        // Level up
        int before = cur;
        int after = cur + 1;
        nodeLevels[node.id] = after;
        unlocked[node.id] = true; // legacy bool

        // gainedValue = netop det level vi købte (for flade stats)
        float gainedValue = 0f;
        if (IsFlatStat(node) && node.valuePerLevel != null && node.valuePerLevel.Count > before)
            gainedValue = node.valuePerLevel[before];

        // Kald effekt-hook (flad stat -> buff, ellers special-case via node.id)
        NodeEffects.ApplyOnLeveled(node, after, gainedValue);

        SaveLevels();
        SaveUnlocksLegacy();
        RefreshAllNodeStates();
        return true;
    }

    public void ShowNodeDetails(NodeData node)
    {
        if (detailPanel != null) detailPanel.Show(node);
    }

    private void RefreshAllNodeStates()
    {
        foreach (var b in nodeBindings)
        {
            if (b.nodeUI == null || b.nodeData == null) continue;

            bool isU = IsUnlocked(b.nodeData.id);
            bool canUpgrade = !IsMaxed(b.nodeData);
            bool avail = (!isU && ArePrerequisitesMet(b.nodeData)) || (isU && canUpgrade);

            b.nodeUI.ForceUnlock(isU);
            b.nodeUI.SetAvailable(avail);
            b.nodeUI.Refresh();
        }
    }

    // -------- Save (levels + legacy) --------
    private const string KEY_LEVELS = "nodeLevels";
    private const string KEY_UNLOCKS = "unlockedNodes";

    private void SaveLevels()
    {
        var parts = new List<string>();
        foreach (var kv in nodeLevels)
            if (kv.Value > 0)
                parts.Add(kv.Key + "=" + kv.Value);
        PlayerPrefs.SetString(KEY_LEVELS, string.Join("|", parts));
        PlayerPrefs.Save();
    }

    private void LoadLevels()
    {
        nodeLevels.Clear();
        string payload = PlayerPrefs.GetString(KEY_LEVELS, "");
        if (string.IsNullOrEmpty(payload)) return;

        var items = payload.Split('|');
        foreach (var it in items)
        {
            var pair = it.Split('=');
            if (pair.Length == 2 && int.TryParse(pair[1], out int lv))
                nodeLevels[pair[0]] = Mathf.Max(0, lv);
        }
    }

    private void SaveUnlocksLegacy()
    {
        string ids = string.Join(",", nodeLevels.Keys); // alle med lv>=1
        PlayerPrefs.SetString(KEY_UNLOCKS, ids);
        PlayerPrefs.Save();
    }

    private void LoadUnlocksLegacy()
    {
        unlocked.Clear();
        string ids = PlayerPrefs.GetString(KEY_UNLOCKS, "");
        if (string.IsNullOrEmpty(ids)) return;
        foreach (var id in ids.Split(','))
            if (!string.IsNullOrEmpty(id))
                unlocked[id] = true;
    }

    [ContextMenu("Reset All Nodes (Testing)")]
    // ... (din nuværende fil uændret, kun ResetAllNodes ændret)
    [ContextMenu("Reset All Nodes (Testing)")]
    public void ResetAllNodes()
    {
        // ryd skill-levels
        unlocked.Clear();
        nodeLevels.Clear();
        PlayerPrefs.DeleteKey(KEY_LEVELS);
        PlayerPrefs.DeleteKey(KEY_UNLOCKS);
        PlayerPrefs.Save();

        // ryd persisterede buff totals
        PlayerBuffManager.ClearPersistentTotals();

        RefreshAllNodeStates();
        Debug.Log("[SkillTreeManager] All nodes reset!");
    }
    // ...

}
