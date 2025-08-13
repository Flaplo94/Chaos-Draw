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

    private ShopItem item;
    private ShopManager shop;
    private int cachedPrice;

    public void Bind(ShopItem s, ShopManager m)
    {
        item = s;
        shop = m;

        // Sæt ikon + tekster
        switch (item.itemType)
        {
            case ShopItemType.Artifact:
                if (item.artifactData != null)
                {
                    if (icon) icon.sprite = item.artifactData.icon;
                    if (nameText) nameText.text =
                        string.IsNullOrEmpty(item.artifactData.artifactName)
                        ? item.artifactData.name
                        : item.artifactData.artifactName;
                    if (descText) descText.text = item.artifactData.description ?? "";
                }
                break;

            case ShopItemType.Buff:
                if (item.buffData != null)
                {
                    if (icon) icon.sprite = item.buffData.icon;
                    if (nameText) nameText.text =
                        string.IsNullOrEmpty(item.buffData.buffName)
                        ? item.buffData.name
                        : item.buffData.buffName;
                    if (descText) descText.text = item.buffData.description ?? "";
                }
                break;

            case ShopItemType.Service:
                if (nameText) nameText.text = item.serviceType.ToString(); // "RemoveCard"
                if (descText) descText.text = "Remove a card from your deck";
                break;
        }

        // Pris + klik
        cachedPrice = item.basePrice;
        if (priceText) priceText.text = cachedPrice.ToString();

        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(OnBuy);
        }

        // Initial interaktivitet ift. guld/eje
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

        bool owned =
            (item.itemType == ShopItemType.Artifact && item.artifactData && PlayerInventory.Instance.Has(item.artifactData)) ||
            (item.itemType == ShopItemType.Buff && item.buffData && PlayerInventory.Instance.Has(item.buffData));

        // Services kan altid købes hvis man har råd; andre kræver også at man ikke allerede ejer dem
        bool interactable = (item.itemType == ShopItemType.Service) ? enough : (enough && !owned);

        if (buyButton) buyButton.interactable = interactable;
    }

    private void OnBuy()
    {
        if (!shop.TryBuy(item, cachedPrice)) return;

        // Efter succes: disable køb for non-service
        if (item.itemType != ShopItemType.Service && buyButton != null)
            buyButton.interactable = false;
    }
}
