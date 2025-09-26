using UnityEngine;
using System.Collections.Generic;

public class PlayerItemManager : MonoBehaviour
{
    public static PlayerItemManager Instance;

    public List<ItemData> ownedItems = new List<ItemData>();
    private readonly HashSet<string> appliedIDs = new HashSet<string>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    public void AddItem(ItemData item)
    {
        if (item == null) return;

        string key = Normalize(item.internalID);
        if (string.IsNullOrWhiteSpace(key))
        {
            Debug.LogWarning("[Item] Missing internalID on item: " + item.name);
            return;
        }
        if (appliedIDs.Contains(key))
        {
            Debug.Log("[Item] Already applied: " + key);
            return;
        }

        ownedItems.Add(item);
        appliedIDs.Add(key);

        ApplyItemEffect(item);

        // Items are always-on  persist totals
        PlayerBuffManager.Instance?.SavePersistentTotals();

        // Optional: update an Items UI bar if you have one
        var ui = FindFirstObjectByType<ItemUIManager>(FindObjectsInactive.Exclude);
        if (ui != null) ui.UpdateItemUI();
    }

    public bool HasItem(string id)
    {
        string nid = Normalize(id);
        return ownedItems.Exists(i => i != null && Normalize(i.internalID) == nid);
    }

    private void ApplyItemEffect(ItemData item)
    {
        var buffs = PlayerBuffManager.Instance;
        if (buffs == null) { Debug.LogError("[Item] PlayerBuffManager not found!"); return; }

        string id = Normalize(item.internalID);

        // EXACTLY like artifacts: switch by ID
        switch (id)
        {
            // --- Examples (edit/extend these to your real item IDs) ---
            case "fuel":
                PlayerBuffManager.Instance.AddRuntimeBonus(BuffData.BuffType.BurnDamage, 0.10f);
                break;

            case "matchbox":
                PlayerBuffManager.Instance.AddRuntimeBonus(BuffData.BuffType.FireDamage, 0.10f);
                break;

            case "tazer":
                PlayerBuffManager.Instance.AddRuntimeBonus(BuffData.BuffType.ThunderDamage, 0.10f);
                break;

            case "filpflopsoffury":
                PlayerBuffManager.Instance.AddRuntimeBonus(BuffData.BuffType.Speed, 0.10f);
                break;

            case "goldnugget":
                PlayerBuffManager.Instance.AddRuntimeBonus(BuffData.BuffType.GoldGain, 0.10f);
                break;

            case "fortifiedcloak":
                PlayerBuffManager.Instance.AddRuntimeBonus(BuffData.BuffType.MaxHP, 0.15f);
                break;
            // Add more hard-coded items here if they need special handling...
            // -------------------------------------------------------------

            default:
                // Fallback: use the SO’s generic definition
                // (this makes most items require NO code change)
                buffs.AddRuntimeBonus(item.buffType, item.value);
                Debug.Log($"[Item] Default applied: {item.itemName} -> {item.buffType} +{item.value}");
                break;
        }
    }

    private static string Normalize(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        s = s.Trim().ToLowerInvariant();
        s = s.Replace(" ", "").Replace("_", "").Replace("-", "");
        return s;
    }
}
