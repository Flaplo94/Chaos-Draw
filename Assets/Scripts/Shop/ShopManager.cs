using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;
using System.Collections.Generic;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance;

    [Header("Data")]
    public ShopCatalog catalog;
    public ShopItem removeCardServiceItem;

    [Header("UI Root")]
    public GameObject windowGroup;     // ShopWindow root
    public Dimmer dimmer;              // helst denne (komponenten)
    public GameObject dimmerGO;        // fallback hvis du ikke vil bruge komponenten
    public Button closeButton;

    [Header("Slots (manual)")]
    public RectTransform[] artifactSlots = new RectTransform[3];
    public RectTransform[] buffSlots = new RectTransform[3];
    public RectTransform serviceSlot;

    [Header("Item UI")]
    public GameObject itemUIPrefab;

    [Header("Behavior")]
    public bool pauseOnOpen = true;
    public KeyCode debugToggleKey = KeyCode.O;

    [Header("Quality of life")]
    public bool autoFindSlots = true;

    public event Action OnClosed;
    public event Action<ShopItem> OnPurchased;

    private readonly List<GameObject> spawned = new List<GameObject>();
    private bool isOpen = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (closeButton != null) closeButton.onClick.AddListener(Close);

        if (windowGroup) windowGroup.SetActive(false);
        if (dimmer) dimmer.InstantOff();
        else if (dimmerGO) dimmerGO.SetActive(false);
    }

    void OnDisable()
    {
        if (pauseOnOpen) Time.timeScale = 1f;
    }

    void Update()
    {
        if (debugToggleKey != KeyCode.None && Input.GetKeyDown(debugToggleKey))
        {
            if (isOpen) Close();
            else Open();
        }

        if (isOpen && Input.GetKeyDown(KeyCode.Escape))
            Close();
    }

    public void Open()
    {
        if (isOpen) return;

        if (catalog == null || itemUIPrefab == null)
        {
            Debug.LogError("[Shop] Missing catalog or itemUIPrefab");
            return;
        }

        if (!ValidateSlots() && autoFindSlots) AutoFindAllSlots();
        if (!ValidateSlots())
        {
            Debug.LogError("[Shop] Parents not assigned (Artifacts/Buffs/Service).");
            return;
        }

        ClearUI();
        BuildSelectionUI();

        if (windowGroup) windowGroup.SetActive(true);

        // Dimmer on
        if (dimmer) dimmer.Show();
        else if (dimmerGO) dimmerGO.SetActive(true);
        else Debug.LogWarning("[Shop] No Dimmer reference set.");

        if (pauseOnOpen) Time.timeScale = 0f;

        isOpen = true;
        Debug.Log("[Shop] Open()");
    }

    public void Close()
    {
        if (!isOpen) return;

        if (windowGroup) windowGroup.SetActive(false);

        // Dimmer off
        if (dimmer) dimmer.Hide();
        else if (dimmerGO) dimmerGO.SetActive(false);

        if (pauseOnOpen) Time.timeScale = 1f;

        ClearUI();
        isOpen = false;
        Debug.Log("[Shop] Close()");
        OnClosed?.Invoke();
    }

    public bool TryBuy(ShopItem item, int price)
    {
        var wallet = Wallet.Instance;
        if (wallet == null) { Debug.LogWarning("[Shop] No Wallet present."); return false; }
        if (!wallet.TrySpend(price)) return false;

        switch (item.itemType)
        {
            case ShopItemType.Artifact:
                if (item.artifactData && PlayerInventory.Instance != null)
                {
                    PlayerInventory.Instance.Add(item.artifactData);
                    Debug.Log($"[Shop] Added Artifact: {item.artifactData.name}");
                }
                break;

            case ShopItemType.Buff:
                if (item.buffData && PlayerInventory.Instance != null)
                {
                    PlayerInventory.Instance.Add(item.buffData);
                    Debug.Log($"[Shop] Added Buff: {item.buffData.name}");
                }
                break;

            case ShopItemType.Service:
                ServiceRunner.Run(item);
                break;
        }

        OnPurchased?.Invoke(item);
        return true;
    }

    // ---------- intern UI opsætning ----------
    private void BuildSelectionUI()
    {
        var inv = PlayerInventory.Instance;

        var allArts = catalog.items
            .Where(i => i && i.itemType == ShopItemType.Artifact && i.artifactData);
        var artsSource = (inv != null) ? allArts.Where(i => !inv.Has(i.artifactData)) : allArts;
        var arts = artsSource.OrderBy(_ => UnityEngine.Random.value).Take(3).ToList();
        if (arts.Count < 3) arts = allArts.OrderBy(_ => UnityEngine.Random.value).Take(3).ToList();

        var allBuffs = catalog.items
            .Where(i => i && i.itemType == ShopItemType.Buff && i.buffData);
        var buffsSource = (inv != null) ? allBuffs.Where(i => !inv.Has(i.buffData)) : allBuffs;
        var buffs = buffsSource.OrderBy(_ => UnityEngine.Random.value).Take(3).ToList();
        if (buffs.Count < 3) buffs = allBuffs.OrderBy(_ => UnityEngine.Random.value).Take(3).ToList();

        for (int i = 0; i < 3 && i < arts.Count && i < artifactSlots.Length; i++)
            SpawnIntoSlot(arts[i], artifactSlots[i]);

        for (int i = 0; i < 3 && i < buffs.Count && i < buffSlots.Length; i++)
            SpawnIntoSlot(buffs[i], buffSlots[i]);

        if (removeCardServiceItem != null && serviceSlot != null)
            SpawnIntoSlot(removeCardServiceItem, serviceSlot);
    }

    private void SpawnIntoSlot(ShopItem item, RectTransform slot)
    {
        if (slot == null || itemUIPrefab == null || item == null) return;
        var go = Instantiate(itemUIPrefab);
        spawned.Add(go);

        var rt = go.transform as RectTransform;
        rt.SetParent(slot, false);
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;

        var ui = go.GetComponent<ShopItemUI>();
        if (ui != null) ui.Bind(item, this);
        else Debug.LogError("[Shop] ItemUIPrefab mangler ShopItemUI-komponent.");
    }

    private void ClearUI()
    {
        for (int i = 0; i < spawned.Count; i++)
            if (spawned[i]) Destroy(spawned[i]);
        spawned.Clear();
    }

    private bool ValidateSlots()
    {
        bool artsOk = artifactSlots != null && artifactSlots.Length >= 3 &&
                      artifactSlots[0] && artifactSlots[1] && artifactSlots[2];
        bool buffsOk = buffSlots != null && buffSlots.Length >= 3 &&
                       buffSlots[0] && buffSlots[1] && buffSlots[2];
        bool serviceOk = serviceSlot != null;
        return artsOk && buffsOk && serviceOk;
    }

    private void AutoFindAllSlots()
    {
        Transform root = windowGroup != null ? windowGroup.transform : transform;

        if ((artifactSlots == null) || artifactSlots.Length < 3 ||
            artifactSlots[0] == null || artifactSlots[1] == null || artifactSlots[2] == null)
        {
            var arts = new List<RectTransform>();
            TryFindThreeSlots(root, "Artifacts", "ArtifactsContent", arts);
            if (arts.Count == 3) artifactSlots = arts.ToArray();
        }

        if ((buffSlots == null) || buffSlots.Length < 3 ||
            buffSlots[0] == null || buffSlots[1] == null || buffSlots[2] == null)
        {
            var buffs = new List<RectTransform>();
            TryFindThreeSlots(root, "Buffs", "BuffsContent", buffs);
            if (buffs.Count == 3) buffSlots = buffs.ToArray();
        }

        if (serviceSlot == null)
        {
            var t = FindDeepChildByLooseName(root, "Service");
            if (t == null) t = FindDeepChildByLooseName(root, "Remove");
            if (t != null) serviceSlot = t.GetComponent<RectTransform>();
        }
    }

    private void TryFindThreeSlots(Transform root, string groupName, string contentName, List<RectTransform> outList)
    {
        var group = FindDeepChildByLooseName(root, groupName);
        if (group == null) group = FindDeepChildByLooseName(root, contentName);
        if (group == null) return;

        outList.Clear();
        for (int i = 0; i < group.childCount && outList.Count < 3; i++)
        {
            var rt = group.GetChild(i).GetComponent<RectTransform>();
            if (rt != null) outList.Add(rt);
        }
    }

    private Transform FindDeepChildByLooseName(Transform root, string contains)
    {
        if (root.name.IndexOf(contains, StringComparison.OrdinalIgnoreCase) >= 0)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            var found = FindDeepChildByLooseName(root.GetChild(i), contains);
            if (found != null) return found;
        }
        return null;
    }
}
