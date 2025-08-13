using UnityEngine;

public enum ShopItemType { Artifact, Buff, Service }
public enum ShopServiceType { None, RemoveCard }

[CreateAssetMenu(fileName = "New Shop Item", menuName = "ChaosDraw/Shop/Item")]
public class ShopItem : ScriptableObject
{
    [Header("Type")]
    public ShopItemType itemType = ShopItemType.Artifact;

    [Header("Data")]
    public ArtifactData artifactData;
    public BuffData buffData;

    [Header("Service")]
    public ShopServiceType serviceType = ShopServiceType.None;

    [Header("Pris")]
    public int basePrice = 50;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (itemType == ShopItemType.Artifact) buffData = null;
        if (itemType == ShopItemType.Buff) artifactData = null;
    }
#endif
}
