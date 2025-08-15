using UnityEngine;

public enum ShopItemType { Artifact, Buff, Service }
public enum ShopServiceType { None, RemoveCard }

[CreateAssetMenu(fileName = "New Shop Item", menuName = "ChaosDraw/Shop/Item")]
public class ShopItem : ScriptableObject
{
    [Header("Type")]
    public ShopItemType itemType = ShopItemType.Artifact;

    [Header("Data (for Artifact/Buff)")]
    public ArtifactData artifactData;
    public BuffData buffData;

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

            case ShopItemType.Buff:
                // Brug ALTID ikon fra BuffData
                return buffData ? buffData.icon : null;

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
            case ShopItemType.Buff:
                if (buffData)
                    return string.IsNullOrEmpty(buffData.buffName) ? buffData.name : buffData.buffName;
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
            case ShopItemType.Buff:
                return buffData ? (buffData.description ?? "") : "";
            case ShopItemType.Service:
                return "Remove a card from your deck.";
        }
        return "";
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // hold data felter eksklusive
        if (itemType == ShopItemType.Artifact) buffData = null;
        if (itemType == ShopItemType.Buff) artifactData = null;
    }
#endif
}
