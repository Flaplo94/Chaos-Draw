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
    [Tooltip("Hvis true skjules navn-tekstens GameObject helt for RemoveCard (bedre layout).")]
    public bool disableNameObjectForRemoveCard = true;

    private ShopItem item;
    private ShopManager shop;
    private int cachedPrice;

    public void Bind(ShopItem s, ShopManager manager)
    {
        item = s;
        shop = manager;
        cachedPrice = item != null ? item.basePrice : 0;

        // --- Titel (skjul for RemoveCard) ---
        if (nameText)
        {
            bool isRemoveCard = (item != null &&
                                 item.itemType == ShopItemType.Service &&
                                 item.serviceType == ShopServiceType.RemoveCard);

            if (isRemoveCard)
            {
                if (disableNameObjectForRemoveCard)
                    nameText.gameObject.SetActive(false);   // skjul helt (bedst m. VerticalLayoutGroup)
                else
                    nameText.text = "";                      // behold pladsen, men tom tekst
            }
            else
            {
                nameText.gameObject.SetActive(true);
                nameText.text = item != null ? item.GetTitle() : "";
            }
        }

        // --- Beskrivelse ---
        if (descText) descText.text = item != null ? item.GetDescription() : "";

        // --- Ikon (Service har typisk ingen) ---
        if (icon)
        {
            var sp = (item != null) ? item.GetIcon() : null;
            icon.sprite = sp;
            icon.enabled = sp != null;       // Undgå hvid boks
            if (sp != null) icon.preserveAspect = true;
        }

        // --- Pris ---
        if (priceText) priceText.text = cachedPrice.ToString();

        // --- Knap ---
        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(OnBuy);
        }

        RefreshInteractable(Wallet.Instance != null ? Wallet.Instance.Gold : 0);
    }

    void OnEnable()
    {
        if (Wallet.Instance != null)
            Wallet.Instance.OnGoldChanged += RefreshInteractable;
    }

    void OnDisable()
    {
        if (Wallet.Instance != null)
            Wallet.Instance.OnGoldChanged -= RefreshInteractable;
    }

    private void RefreshInteractable(int gold)
    {
        bool enough = gold >= cachedPrice;

        bool owned = false;
        var inv = PlayerInventory.Instance; // kan være null
        if (inv != null && item != null)
        {
            if (item.itemType == ShopItemType.Artifact && item.artifactData != null)
                owned = inv.Has(item.artifactData);
            else if (item.itemType == ShopItemType.Buff && item.buffData != null)
                owned = inv.Has(item.buffData);
        }

        // Services kan altid købes, hvis der er penge nok
        bool interactable = (item != null && item.itemType == ShopItemType.Service) ? enough : (enough && !owned);
        if (buyButton) buyButton.interactable = interactable;
    }

    private void OnBuy()
    {
        if (shop == null || item == null) return;
        if (!shop.TryBuy(item, cachedPrice)) return;

        if (item.itemType != ShopItemType.Service && buyButton != null)
            buyButton.interactable = false;
    }
}
