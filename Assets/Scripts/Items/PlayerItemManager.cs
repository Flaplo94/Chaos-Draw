using UnityEngine;
using System.Collections.Generic;

public class PlayerItemManager : MonoBehaviour
{
    public static PlayerItemManager Instance;

    [Header("Owned Items (runtime & save)")]
    public List<ItemData> ownedItems = new List<ItemData>();

    // Fast duplicate guard based on normalized IDs
    private readonly HashSet<string> appliedIDs = new HashSet<string>();

    // --- Ring Of Adaptive Power (dynamic per-item scaling) ---
    private bool adaptiveRingOwned = false;
    private float adaptiveApplied = 0f;
    private float adaptivePerItem = 0.05f;    // how much Damage we've currently applied via the ring

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void OnEnable()
    {
        // Recompute whenever inventory changes (new items added)
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnInventoryChanged += OnInventoryChanged;
            PlayerInventory.Instance.OnItemAdded += OnItemAdded;
        }
    }

    void OnDisable()
    {
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnInventoryChanged -= OnInventoryChanged;
            PlayerInventory.Instance.OnItemAdded -= OnItemAdded;
        }
    }

    // Called when any item gets added/removed (we only add in your current game, but safe)
    private void OnInventoryChanged()
    {
        if (adaptiveRingOwned) RecomputeAdaptivePower();
    }

    // Called specifically when an item is added
    private void OnItemAdded(ItemData _)
    {
        if (adaptiveRingOwned) RecomputeAdaptivePower();
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
        if (buffs == null || item == null) return;

        string id = Normalize(item.internalID);
        float v = item.value; 

        switch (id)
        {
            case "fuel":             
                buffs.AddRuntimeBonus(BuffData.BuffType.BurnDamage, v);
                break;

            case "matchbox":        
                buffs.AddRuntimeBonus(BuffData.BuffType.FireDamage, v);
                break;

            case "tazer":              
                buffs.AddRuntimeBonus(BuffData.BuffType.ThunderDamage, v);
                break;

            case "filpflopsoffury":   
                buffs.AddRuntimeBonus(BuffData.BuffType.Speed, v);
                break;

            case "goldnugget":      
                buffs.AddRuntimeBonus(BuffData.BuffType.GoldGain, v);
                break;

            case "manafilledsleeves": 
                buffs.AddRuntimeBonus(BuffData.BuffType.ManaCostFlat, v);
                break;

            case "dealerglove":       
                buffs.AddRuntimeBonus(BuffData.BuffType.ExtraProjectile, v);
                break;

            case "glovesofspeed":     
                buffs.AddRuntimeBonus(BuffData.BuffType.AttackSpeed, v);
                break;

            case "vampiricring":
                buffs.AddRuntimeBonus(BuffData.BuffType.Lifesteal, v);
                break;

            case "ringofadaptivepower": 
                adaptiveRingOwned = true;
                adaptivePerItem = Mathf.Max(0f, v); 
                RecomputeAdaptivePower();
                break;

            default:
               
                buffs.AddRuntimeBonus(item.buffType, v);
                break;
        }
    }

    /// <summary>
    /// Recomputes Ring Of Adaptive Power’s bonus as (+5% Damage) * (# of active items).
    /// Applies only the delta against what's already applied, so stacking stays correct.
    /// Counts all items in PlayerInventory (including the ring itself).
    /// </summary>
    private void RecomputeAdaptivePower()
    {
        if (!adaptiveRingOwned) return;
        var pbm = PlayerBuffManager.Instance;
        var inv = PlayerInventory.Instance;
        if (pbm == null || inv == null) return;

        int itemCount = inv.items != null ? inv.items.Count : 0;
        float newTotal = itemCount * adaptivePerItem; 

        float delta = newTotal - adaptiveApplied;
        if (Mathf.Abs(delta) > 0.0001f)
        {
            pbm.AddRuntimeBonus(BuffData.BuffType.Damage, delta);
            adaptiveApplied = newTotal;
            
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
