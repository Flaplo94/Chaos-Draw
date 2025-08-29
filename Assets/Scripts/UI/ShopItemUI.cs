using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopItemUI : MonoBehaviour
{
    [Header("UI")]
    public Image icon;
    public TMP_Text nameText;
    public TMP_Text descText;
    public TMP_Text priceText;
    public Button buyButton;

    [Header("Layout options")]
    [Tooltip("Skjul hele nameText GameObject for RemoveCard.")]
    public bool disableNameObjectForRemoveCard = true;

    [Tooltip("Tekst, der vises når varen er ejet (Artifact/Buff).")]
    public string ownedLabel = "Owned";

    private ShopItem item;
    private ShopManager shop;
    private int cachedPrice;

    // Tracker om vi er subscribet til Wallet-event
    private bool subscribedToWallet = false;

    // ---------- Lifecycle ----------
    void Awake()
    {
        // Sikrer vi ikke dobbelte listeners hvis Bind kaldes igen
        if (buyButton != null) buyButton.onClick.RemoveAllListeners();
    }

    void OnEnable()
    {
        SubscribeWallet();
        // Init UI hvis item allerede er bundet
        if (item != null)
            RefreshAll();
        else
            RefreshInteractable(GetGold());
    }

    void OnDisable()
    {
        UnsubscribeWallet();
        if (buyButton != null) buyButton.onClick.RemoveAllListeners();
    }

    void OnDestroy()
    {
        UnsubscribeWallet();
    }

    // ---------- Public API ----------
    public void Bind(ShopItem s, ShopManager manager)
    {
        item = s;
        shop = manager;
        cachedPrice = (item != null) ? item.basePrice : 0;

        // Titel (skjul for RemoveCard)
        if (nameText)
        {
            bool isRemoveCard = (item != null &&
                                 item.itemType == ShopItemType.Service &&
                                 item.serviceType == ShopServiceType.RemoveCard);

            if (isRemoveCard && disableNameObjectForRemoveCard)
            {
                nameText.gameObject.SetActive(false);
            }
            else
            {
                if (!nameText.gameObject.activeSelf) nameText.gameObject.SetActive(true);
                nameText.text = (item != null) ? item.GetTitle() : "";
            }
        }

        // Beskrivelse
        if (descText) descText.text = (item != null) ? item.GetDescription() : "";

        // Ikon (Service = typisk intet)
        if (icon)
        {
            Sprite sp = (item != null) ? item.GetIcon() : null;

            // RemoveCard: intet ikon
            bool hideIconForRemoveCard = (item != null &&
                                          item.itemType == ShopItemType.Service &&
                                          item.serviceType == ShopServiceType.RemoveCard);

            if (hideIconForRemoveCard) sp = null;

            icon.sprite = sp;
            icon.enabled = sp != null;
            if (sp != null) icon.preserveAspect = true;
        }

        // Pris
        if (priceText) priceText.text = cachedPrice.ToString();

        // Knap
        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(OnBuy);
        }

        // Starttilstand
        RefreshAll();
        SubscribeWallet(); // sikrer vi lytter, selv hvis Bind blev kaldt før OnEnable
    }

    // ---------- Intern logik ----------
    private void OnBuy()
    {
        if (shop == null || item == null) return;

        if (!shop.TryBuy(item, cachedPrice))
            return;

        // Hvis det er en engangsvare (Artifact/Buff), sæt solgt/owned
        if (item.itemType != ShopItemType.Service)
        {
            MarkOwned();
        }

        // Opdater interaktion efter køb
        RefreshAll();
    }

    private void MarkOwned()
    {
        // Disable knap og vis "Owned"
        if (buyButton) buyButton.interactable = false;
        if (priceText) priceText.text = ownedLabel;

        // Titel kan forblive, men vi markerer visuelt via prisText
    }

    private void RefreshAll()
    {
        // Ejet?
        bool owned = IsOwned();

        // Opdatér prislabel ved owned
        if (priceText)
            priceText.text = owned ? ownedLabel : cachedPrice.ToString();

        // Interactable afhænger af guld og owned (services ser kun på guld)
        RefreshInteractable(GetGold());
    }

    private void RefreshInteractable(int gold)
    {
        if (buyButton == null) return;
        if (item == null) { buyButton.interactable = false; return; }

        bool enough = gold >= cachedPrice;

        if (item.itemType == ShopItemType.Service)
        {
            buyButton.interactable = enough;
            return;
        }

        // Artifact/Buff: må ikke kunne købes hvis allerede ejet
        bool owned = IsOwned();
        buyButton.interactable = enough && !owned;
    }

    private bool IsOwned()
    {
        var inv = PlayerInventory.Instance;
        if (inv == null || item == null) return false;

        if (item.itemType == ShopItemType.Artifact && item.artifactData != null)
            return inv.Has(item.artifactData);

        if (item.itemType == ShopItemType.Buff && item.buffData != null)
            return inv.Has(item.buffData);

        return false; // Services kan ikke "ejes"
    }

    private int GetGold()
    {
        return (Wallet.Instance != null) ? Wallet.Instance.CurrentGold : 0;
    }

    // ---------- Wallet event subscription ----------
    private void SubscribeWallet()
    {
        if (subscribedToWallet) return;
        if (Wallet.Instance == null) return;

        Wallet.Instance.OnGoldChanged += RefreshInteractable;
        subscribedToWallet = true;

        // Synk UI med nuværende guld
        RefreshInteractable(Wallet.Instance.CurrentGold);
    }

    private void UnsubscribeWallet()
    {
        if (!subscribedToWallet) return;
        if (Wallet.Instance != null)
            Wallet.Instance.OnGoldChanged -= RefreshInteractable;
        subscribedToWallet = false;
    }
}
