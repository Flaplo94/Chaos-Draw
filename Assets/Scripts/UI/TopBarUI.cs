using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TopBarUI : MonoBehaviour
{
    [Header("Data / Systems")]
    public ShopCatalog catalog;
    public WaveManager waveManager;

    [Header("UI")]
    public TMP_Text goldText;          // fx GoldGroup/GoldValue
    public TMP_Text waveText;          // fx WaveGroup/WaveValue
    public Transform artifactsParent;  // fx ArtifactsContent
    public Transform buffsParent;      // fx BuffsConent
    public GameObject iconPrefab;      // lille Image-prefab 32–40 px

    [Header("Options")]
    public bool hideEmptySections = false;
    public int fallbackIconSize = 32;

    private readonly List<GameObject> _artifactIcons = new();
    private readonly List<GameObject> _buffIcons = new();

    // tracking af shop-hook
    private Coroutine shopHookCo;
    private bool shopEventsHooked = false;

    void OnEnable()
    {
        // GOLD
        if (Wallet.Instance != null)
        {
            Wallet.Instance.OnGoldChanged += HandleGoldChanged;
            HandleGoldChanged(Wallet.Instance.Gold);
        }
        else HandleGoldChanged(0);

        // WAVE
        if (waveManager != null)
        {
            waveManager.OnWaveStarted += HandleWaveStarted;
            waveManager.OnWaveCompleted += HandleWaveCompleted;
            UpdateWave(waveManager.GetCurrentWaveNumber());
        }
        else UpdateWave(0);

        // SHOP – prøv at hooke nu; hvis Instance ikke findes endnu,
        // venter vi med en coroutine til den er klar.
        TryHookShopEvents();
        if (!shopEventsHooked)
            shopHookCo = StartCoroutine(HookShopWhenReady());

        // første fulde scan
        RefreshInventorySections();
    }

    void OnDisable()
    {
        if (Wallet.Instance != null)
            Wallet.Instance.OnGoldChanged -= HandleGoldChanged;

        if (waveManager != null)
        {
            waveManager.OnWaveStarted -= HandleWaveStarted;
            waveManager.OnWaveCompleted -= HandleWaveCompleted;
        }

        UnhookShopEvents();

        if (shopHookCo != null)
        {
            StopCoroutine(shopHookCo);
            shopHookCo = null;
        }
    }

    // ---------- SHOP HOOKING ----------
    private void TryHookShopEvents()
    {
        if (shopEventsHooked) return;
        if (ShopManager.Instance == null) return;

        ShopManager.Instance.OnPurchased += HandlePurchased;
        ShopManager.Instance.OnClosed += RefreshInventorySections;
        shopEventsHooked = true;
        // Debug.Log("[TopBar] Hooked Shop events");
    }

    private IEnumerator HookShopWhenReady()
    {
        while (ShopManager.Instance == null)
            yield return null;

        TryHookShopEvents();
        shopHookCo = null;
    }

    private void UnhookShopEvents()
    {
        if (!shopEventsHooked) return;
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.OnPurchased -= HandlePurchased;
            ShopManager.Instance.OnClosed -= RefreshInventorySections;
        }
        shopEventsHooked = false;
    }

    // ---------- Events ----------
    void HandleGoldChanged(int value) { if (goldText) goldText.text = value.ToString(); }
    void HandleWaveStarted(int wave) { UpdateWave(wave); }
    void HandleWaveCompleted(int _) { /* optional */ }

    // KALDES NÅR NOGET KØBES I SHOPPEN
    void HandlePurchased(ShopItem item)
    {
        if (item == null) return;

        if (item.itemType == ShopItemType.Artifact && item.artifactData != null)
        {
            Debug.Log($"[TopBar] Purchased Artifact: {item.artifactData.name}");
            if (artifactsParent && iconPrefab)
                _artifactIcons.Add(SpawnIcon(artifactsParent, item.artifactData.icon));
        }
        else if (item.itemType == ShopItemType.Buff && item.buffData != null)
        {
            Debug.Log($"[TopBar] Purchased Buff: {item.buffData.name}");
            if (buffsParent && iconPrefab)
                _buffIcons.Add(SpawnIcon(buffsParent, item.buffData.icon));
        }

        ApplySectionVisibility();
    }

    void UpdateWave(int wave)
    {
        if (!waveText) return;
        waveText.text = (wave > 0) ? wave.ToString() : "-";
    }

    // ---------- Full refresh (fallback ved enable/close) ----------
    public void RefreshInventorySections()
    {
        Clear(_artifactIcons);
        Clear(_buffIcons);

        var inv = PlayerInventory.Instance;
        if (inv == null || catalog == null)
        {
            ApplySectionVisibility();
            return;
        }

        var allArtifacts = catalog.items
            .Where(i => i && i.itemType == ShopItemType.Artifact && i.artifactData)
            .Select(i => i.artifactData).Distinct().ToList();

        var allBuffs = catalog.items
            .Where(i => i && i.itemType == ShopItemType.Buff && i.buffData)
            .Select(i => i.buffData).Distinct().ToList();

        foreach (var a in allArtifacts)
            if (inv.Has(a))
                _artifactIcons.Add(SpawnIcon(artifactsParent, a.icon));

        foreach (var b in allBuffs)
            if (inv.Has(b))
                _buffIcons.Add(SpawnIcon(buffsParent, b.icon));

        ApplySectionVisibility();
    }

    // ---------- Ikon helper ----------
    GameObject SpawnIcon(Transform parent, Sprite sprite)
    {
        if (!parent || !iconPrefab) return null;

        var go = Instantiate(iconPrefab, parent, false);

        var img = go.GetComponentInChildren<Image>();
        if (img == null) img = go.AddComponent<Image>();

        var rt = img.GetComponent<RectTransform>();
        if (rt != null && (Mathf.Approximately(rt.rect.width, 0f) || Mathf.Approximately(rt.rect.height, 0f)))
            rt.sizeDelta = new Vector2(fallbackIconSize, fallbackIconSize);

        var le = go.GetComponent<LayoutElement>();
        if (le == null) le = go.AddComponent<LayoutElement>();
        if (le.preferredWidth <= 0f) le.preferredWidth = fallbackIconSize;
        if (le.preferredHeight <= 0f) le.preferredHeight = fallbackIconSize;
        le.flexibleWidth = 0f; le.flexibleHeight = 0f;

        img.sprite = sprite;
        img.enabled = sprite != null;
        if (sprite) img.preserveAspect = true;

        return go;
    }

    void ApplySectionVisibility()
    {
        if (!hideEmptySections) return;
        if (artifactsParent) artifactsParent.gameObject.SetActive(_artifactIcons.Count > 0);
        if (buffsParent) buffsParent.gameObject.SetActive(_buffIcons.Count > 0);
    }

    void Clear(List<GameObject> list)
    {
        foreach (var go in list) if (go) Destroy(go);
        list.Clear();
    }
}
