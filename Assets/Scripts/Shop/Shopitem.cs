using UnityEngine;

public enum ShopItemType { Artifact, Item, Service }
public enum ShopServiceType { None, RemoveCard }

[CreateAssetMenu(fileName = "New Shop Item", menuName = "ChaosDraw/Shop/Item")]
public class ShopItem : ScriptableObject
{
    [Header("Type")]
    public ShopItemType itemType = ShopItemType.Artifact;

    [Header("Data (for Artifact/Item)")]
    public ArtifactData artifactData;
    public ItemData itemData;

    [Header("Service (for Service type)")]
    public ShopServiceType serviceType = ShopServiceType.None;
    public Sprite serviceIcon; // brug kun hvis du VIL have ikon på service

    [Header("Pris")]
    public int basePrice = 50;

    [Header("Shop Display Overrides (valgfrit)")]
    public string titleOverride;
    [TextArea] public string descriptionOverride;
    public Sprite iconOverride; // ignoreres for Artifact/Buff i GetIcon()

    // ---------- Helpers brugt af UI ----------
    public Sprite GetIcon()
    {
        switch (itemType)
        {
            case ShopItemType.Artifact:
                // Brug ALTID ikon fra ArtifactData
                return artifactData ? artifactData.icon : null;

            case ShopItemType.Item:
                // Brug ALTID ikon fra BuffData
                return itemData ? itemData.icon : null;

            case ShopItemType.Service:
                // Kun vis ikon hvis serviceIcon er sat; ellers ingen ikon
                return serviceIcon;
        }
        return null;
    }

    public string GetTitle()
    {
        if (!string.IsNullOrWhiteSpace(titleOverride)) return titleOverride;

        switch (itemType)
        {
            case ShopItemType.Artifact:
                if (artifactData)
                    return string.IsNullOrEmpty(artifactData.artifactName) ? artifactData.name : artifactData.artifactName;
                break;
            case ShopItemType.Item:
                if (itemData)
                    return string.IsNullOrEmpty(itemData.itemName) ? itemData.name : itemData.itemName;
                break;
            case ShopItemType.Service:
                return serviceType.ToString();
        }
        return name;
    }

    public string GetDescription()
    {
        if (!string.IsNullOrWhiteSpace(descriptionOverride)) return descriptionOverride;

        switch (itemType)
        {
            case ShopItemType.Artifact:
                return artifactData ? (artifactData.description ?? "") : "";
            case ShopItemType.Item:
                return itemData ? (itemData.description ?? "") : "";
            case ShopItemType.Service:
                return "Remove a card from your deck.";
        }
        return "";
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // hold data felter eksklusive
        if (itemType == ShopItemType.Artifact) itemData = null;
        if (itemType == ShopItemType.Item) artifactData = null;
    }
#endif
}
