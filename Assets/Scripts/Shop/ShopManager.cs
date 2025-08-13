using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance;

    [Header("Data")]
    public ShopCatalog catalog;
    public ShopItem removeCardServiceItem;   // Service: RemoveCard

    [Header("UI")]
    public GameObject panelRoot;             // Peg på UI/Panels/ShopPanel
    public Transform uiParent;               // Peg på UI/Panels/ShopPanel/Content
    public GameObject itemUIPrefab;          // ShopItemCard (med ShopItemUI)

    // Signal når shop lukkes (bruges af WaveManager)
    public System.Action OnClosed;

    private readonly List<ShopItem> current = new();

    void Awake()
    {
        if (Instance == null) Instance = this; else Destroy(gameObject);
        // Debug
        // Debug.Log("[Shop] Awake");
    }

    public void Open()
    {
        // Debug
        Debug.Log("[Shop] Open() called");

        if (catalog == null)
        {
            Debug.LogError("[Shop] Catalog is NULL");
            return;
        }
        if (uiParent == null)
        {
            Debug.LogError("[Shop] uiParent is NULL (should be ShopPanel/Content)");
            return;
        }
        if (itemUIPrefab == null)
        {
            Debug.LogWarning("[Shop] itemUIPrefab is NULL - viser tom shop");
        }

        // Ryd tidligere kort i UI
        for (int i = uiParent.childCount - 1; i >= 0; i--)
            Destroy(uiParent.GetChild(i).gameObject);
        current.Clear();

        // 3 artifacts (helst ikke-ejede, ellers fallback)
        var arts = catalog.items
            .Where(i => i.itemType == ShopItemType.Artifact && i.artifactData != null)
            .Where(i => !PlayerInventory.Instance.Has(i.artifactData))
            .OrderBy(_ => Random.value).Take(3).ToList();

        if (arts.Count < 3)
        {
            arts = catalog.items
                .Where(i => i.itemType == ShopItemType.Artifact && i.artifactData != null)
                .OrderBy(_ => Random.value).Take(3).ToList();
        }
        current.AddRange(arts);

        // 3 buffs (samme)
        var buffs = catalog.items
            .Where(i => i.itemType == ShopItemType.Buff && i.buffData != null)
            .Where(i => !PlayerInventory.Instance.Has(i.buffData))
            .OrderBy(_ => Random.value).Take(3).ToList();

        if (buffs.Count < 3)
        {
            buffs = catalog.items
                .Where(i => i.itemType == ShopItemType.Buff && i.buffData != null)
                .OrderBy(_ => Random.value).Take(3).ToList();
        }
        current.AddRange(buffs);

        // Service: Remove Card
        if (removeCardServiceItem != null) current.Add(removeCardServiceItem);

        // Byg UI
        foreach (var it in current)
        {
            if (itemUIPrefab == null) break;
            var go = Instantiate(itemUIPrefab, uiParent);
            var ui = go.GetComponent<ShopItemUI>();
            if (ui != null) ui.Bind(it, this);
        }

        // Tænd hele panelet
        var root = panelRoot != null ? panelRoot
                 : (uiParent != null ? uiParent.transform.parent?.gameObject : null);

        if (root == null)
        {
            Debug.LogWarning("[Shop] panelRoot not set (using uiParent only)");
            uiParent.gameObject.SetActive(true);
        }
        else
        {
            root.SetActive(true);
        }

        Time.timeScale = 0f;
        Debug.Log($"[Shop] Visible. Items: {current.Count}");
    }

    public void Close()
    {
        var root = panelRoot != null ? panelRoot
                 : (uiParent != null ? uiParent.transform.parent?.gameObject : null);

        if (root != null) root.SetActive(false);
        else if (uiParent != null) uiParent.gameObject.SetActive(false);

        Time.timeScale = 1f;
        OnClosed?.Invoke();
        Debug.Log("[Shop] Close()");
    }

    public bool TryBuy(ShopItem item, int priceFromUI)
    {
        if (Wallet.Instance == null) return false;
        if (!Wallet.Instance.TrySpend(priceFromUI)) return false;

        switch (item.itemType)
        {
            case ShopItemType.Artifact:
                if (item.artifactData) PlayerInventory.Instance.Add(item.artifactData);
                break;
            case ShopItemType.Buff:
                if (item.buffData) PlayerInventory.Instance.Add(item.buffData);
                break;
            case ShopItemType.Service:
                ServiceRunner.Run(item);
                break;
        }
        return true;
    }
}
