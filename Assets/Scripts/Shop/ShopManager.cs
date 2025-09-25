using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;
using System.Collections.Generic;
using ChaosDraw.SkillTree; // til NodeData og SkillTreeManager

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance;

    [Header("Data")]
    public ShopCatalog catalog;
    public ShopItem removeCardServiceItem;

    [Header("Gating")]
    [Tooltip("Denne skilltree-node skal være unlocked for at vise artifacts i shoppen.")]
    public NodeData artifactUnlockNode;

    [Header("UI Root")]
    public GameObject windowGroup;     // ShopWindow root
    public Dimmer dimmer;              // helst denne (komponenten)
    public GameObject dimmerGO;        // fallback hvis du ikke vil bruge komponenten
    public Button closeButton;

    [Header("Slots (manual)")]
    public RectTransform[] artifactSlots = new RectTransform[3];
    public RectTransform[] itemSlots = new RectTransform[3];
    public RectTransform serviceSlot;

    [Header("Labels")]
    [Tooltip("(Valgfri) Label over artifact-kolonnen der skal skjules indtil unlocked.")]
    public GameObject artifactLabel;

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
        if (pauseOnOpen) PauseManager.ForceUnpause();
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
            Debug.LogError("[Shop] Parents not assigned (Artifacts/Items/Service).");
            return;
        }

        ClearUI();
        BuildSelectionUI();

        if (windowGroup) windowGroup.SetActive(true);

        // Dimmer on
        if (dimmer) dimmer.Show();
        else if (dimmerGO) dimmerGO.SetActive(true);
        else Debug.LogWarning("[Shop] No Dimmer reference set.");

        if (pauseOnOpen) PauseManager.RequestPause();

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

        if (pauseOnOpen) PauseManager.ReleasePause();

        ClearUI();
        isOpen = false;
        Debug.Log("[Shop] Close()");
        OnClosed?.Invoke();
    }

    // wrappers til bakkombat (ShopDebugKey.cs bruger disse navne)
    public void OpenShop() => Open();
    public void CloseShop() => Close();

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
                    PlayerInventory.Instance.AddArtifact(item.artifactData);
                    Debug.Log($"[Shop] Bought Artifact: {item.artifactData.artifactName}");
                }
                break;

            case ShopItemType.Item:
                if (item.itemData && PlayerInventory.Instance != null)
                {
                    PlayerInventory.Instance.AddItem(item.itemData);
                    Debug.Log($"[Shop] Bought Item: {item.itemData.itemName}");
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
        bool canShowArtifacts = HasArtifactsAccess();
        SetArtifactsColumnActive(canShowArtifacts);

        // Artifacts (kun hvis unlocked)
        if (canShowArtifacts)
        {
            var allArts = catalog.items
                .Where(i => i && i.itemType == ShopItemType.Artifact && i.artifactData);
            var arts = allArts.OrderBy(_ => UnityEngine.Random.value).Take(3).ToList();

            for (int i = 0; i < 3 && i < arts.Count && i < artifactSlots.Length; i++)
                SpawnIntoSlot(arts[i], artifactSlots[i]);
        }

        // Buffs (uberørt)
        var allBuffs = catalog.items
            .Where(i => i && i.itemType == ShopItemType.Item && i.itemData);
        var buffs = allBuffs.OrderBy(_ => UnityEngine.Random.value).Take(3).ToList();

        for (int i = 0; i < 3 && i < buffs.Count && i < itemSlots.Length; i++)
            SpawnIntoSlot(buffs[i], itemSlots[i]);

        // Service (uberørt)
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

        // Buff slots er valgfrie: enten alle sat, eller ingen
        bool buffsOk = itemSlots == null || itemSlots.Length == 0 ||
                       (itemSlots.Length >= 3 &&
                        itemSlots[0] && itemSlots[1] && itemSlots[2]);

        // Service stadig påkrævet
        bool serviceOk = serviceSlot != null;

        return artsOk && buffsOk && serviceOk;
    }

    private void AutoFindAllSlots()
    {
        var slots = GetComponentsInChildren<RectTransform>();
        artifactSlots = slots.Where(s => s.name.Contains("Artifact")).ToArray();
        itemSlots = slots.Where(s => s.name.Contains("Item")).ToArray();
        serviceSlot = slots.FirstOrDefault(s => s.name.Contains("Service"));
    }

    // ---------- Artifact gating helpers ----------
    private bool HasArtifactsAccess()
    {
        // Mangler reference? => ingen artifacts
        if (artifactUnlockNode == null) return false;

        // 1) Brug den autoritative SkillTreeManager, hvis den findes i scenen
        if (SkillTreeManager.Instance != null)
            return SkillTreeManager.Instance.IsUnlocked(artifactUnlockNode.id);

        // 2) Fallback: læs samme PlayerPrefs-payload som SkillTreeManager bruger ("nodeLevels")
        // Format: "id=level|id=level|..."
        string payload = PlayerPrefs.GetString("nodeLevels", "");
        if (string.IsNullOrEmpty(payload)) return false;

        var items = payload.Split('|');
        for (int i = 0; i < items.Length; i++)
        {
            var pair = items[i].Split('=');
            if (pair.Length == 2 && pair[0] == artifactUnlockNode.id && int.TryParse(pair[1], out int lv))
                return lv > 0;
        }
        return false;
    }

    private void SetArtifactsColumnActive(bool active)
    {
        // toggle label
        if (artifactLabel != null) artifactLabel.SetActive(active);

        // toggle kolonne/slots
        if (artifactSlots == null) return;
        for (int i = 0; i < artifactSlots.Length; i++)
            if (artifactSlots[i] != null)
                artifactSlots[i].gameObject.SetActive(active);
    }
}
