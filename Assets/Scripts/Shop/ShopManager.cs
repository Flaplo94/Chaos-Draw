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
    public GameObject windowGroup;          // ShopWindow (selve panelet)
    public GameObject dimmer;               // Mørk lag (valgfrit)
    public Button closeButton;              // X-knap

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
    public bool autoFindSlots = true;       // Finder slots automatisk ved navn

    // Callback andre systemer (WaveManager) lytter på
    public Action OnClosed;

    private readonly List<GameObject> spawned = new List<GameObject>();

    void Awake()
    {
        if (Instance == null) Instance = this; else { Destroy(gameObject); return; }

        if (closeButton != null) closeButton.onClick.AddListener(Close);

        // Start med vindue skjult, så vi undgår at det står åbent ved Play
        if (windowGroup) windowGroup.SetActive(false);
        if (dimmer) dimmer.SetActive(false);

        Debug.Log("[Shop] Awake");
    }

    void Update()
    {
        if (debugToggleKey != KeyCode.None && Input.GetKeyDown(debugToggleKey))
        {
            if (windowGroup != null && windowGroup.activeSelf) Close();
            else Open();
        }

        // Luk på Escape
        if (windowGroup != null && windowGroup.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            Close();
    }

    public void Open()
    {
        if (catalog == null || itemUIPrefab == null)
        {
            Debug.LogError("[Shop] Missing catalog or itemUIPrefab");
            return;
        }

        // Safety: find slots hvis ikke sat
        if (!ValidateSlots() && autoFindSlots)
            AutoFindAllSlots();

        if (!ValidateSlots())
        {
            Debug.LogError("[Shop] Parents not assigned (Artifacts/Buffs/Service).");
            return;
        }

        ClearUI();

        // --- Hent PlayerInventory & Wallet sikkert (kan være null tidligt i spillet) ---
        var inv = PlayerInventory.Instance; // kan være null
        var wallet = Wallet.Instance;       // kan være null (OK – vi håndterer det i UI)

        // 3 artifacts (helst ikke-ejede) – hvis inv==null, så filtrer ikke på ownership
        var allArts = catalog.items
            .Where(i => i != null && i.itemType == ShopItemType.Artifact && i.artifactData != null);

        var artsSource = (inv != null)
            ? allArts.Where(i => !inv.Has(i.artifactData))
            : allArts;

        var arts = artsSource
            .OrderBy(_ => UnityEngine.Random.value)
            .Take(3)
            .ToList();

        // Fallback hvis der ikke var nok
        if (arts.Count < 3)
        {
            arts = allArts
                .OrderBy(_ => UnityEngine.Random.value)
                .Take(3)
                .ToList();
        }

        // 3 buffs
        var allBuffs = catalog.items
            .Where(i => i != null && i.itemType == ShopItemType.Buff && i.buffData != null);

        var buffsSource = (inv != null)
            ? allBuffs.Where(i => !inv.Has(i.buffData))
            : allBuffs;

        var buffs = buffsSource
            .OrderBy(_ => UnityEngine.Random.value)
            .Take(3)
            .ToList();

        if (buffs.Count < 3)
        {
            buffs = allBuffs
                .OrderBy(_ => UnityEngine.Random.value)
                .Take(3)
                .ToList();
        }

        // Spawn i slots
        for (int i = 0; i < 3 && i < arts.Count && i < artifactSlots.Length; i++)
            SpawnIntoSlot(arts[i], artifactSlots[i]);

        for (int i = 0; i < 3 && i < buffs.Count && i < buffSlots.Length; i++)
            SpawnIntoSlot(buffs[i], buffSlots[i]);

        if (removeCardServiceItem != null && serviceSlot != null)
            SpawnIntoSlot(removeCardServiceItem, serviceSlot);

        // Vis vindue
        if (dimmer != null) dimmer.SetActive(true);
        if (windowGroup != null) windowGroup.SetActive(true);
        if (pauseOnOpen) Time.timeScale = 0f;

        Debug.Log("[Shop] Open -> items shown: " + (arts.Count + buffs.Count + (removeCardServiceItem ? 1 : 0)));
    }
    //Close
    public void Close()
    {
        if (windowGroup != null) windowGroup.SetActive(false);
        if (dimmer != null) dimmer.SetActive(false);
        if (pauseOnOpen) Time.timeScale = 1f;

        ClearUI();
        OnClosed?.Invoke();

        Debug.Log("[Shop] Close");
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
                    PlayerInventory.Instance.Add(item.artifactData);
                break;
            case ShopItemType.Buff:
                if (item.buffData && PlayerInventory.Instance != null)
                    PlayerInventory.Instance.Add(item.buffData);
                break;
            case ShopItemType.Service:
                ServiceRunner.Run(item);
                break;
        }
        return true;
    }

    private void SpawnIntoSlot(ShopItem item, RectTransform slot)
    {
        if (slot == null || itemUIPrefab == null || item == null) return;

        var go = Instantiate(itemUIPrefab);
        spawned.Add(go);

        var rt = go.transform as RectTransform;
        rt.SetParent(slot, false);

        // fyld hele slot-rect
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
        foreach (var go in spawned)
            if (go) Destroy(go);
        spawned.Clear();
    }

    private bool ValidateSlots()
    {
        bool artsOk = artifactSlots != null && artifactSlots.Length >= 3 &&
                      artifactSlots.All(t => t != null);
        bool buffsOk = buffSlots != null && buffSlots.Length >= 3 &&
                       buffSlots.All(t => t != null);
        bool serviceOk = serviceSlot != null;
        return artsOk && buffsOk && serviceOk;
    }

    private void AutoFindAllSlots()
    {
        if (windowGroup == null) return;

        // Vi forventer: windowGroup/Body/LeftColoumn og /RightColoumn
        var body = windowGroup.transform.Find("Body");
        if (body == null) return;

        var left = body.Find("LeftColoumn");
        var right = body.Find("RightColoumn");

        if (left != null)
        {
            var a1 = left.Find("ArtifactSlot1") as RectTransform;
            var a2 = left.Find("ArtifactSlot2") as RectTransform;
            var a3 = left.Find("ArtifactSlot3") as RectTransform;

            var b1 = left.Find("BuffSlot1") as RectTransform;
            var b2 = left.Find("BuffSlot2") as RectTransform;
            var b3 = left.Find("BuffSlot3") as RectTransform;

            if (a1 && a2 && a3)
                artifactSlots = new RectTransform[] { a1, a2, a3 };

            if (b1 && b2 && b3)
                buffSlots = new RectTransform[] { b1, b2, b3 };
        }

        if (right != null)
        {
            var s = right.Find("ServiceSlot") as RectTransform;
            if (s) serviceSlot = s;
        }
    }
}
