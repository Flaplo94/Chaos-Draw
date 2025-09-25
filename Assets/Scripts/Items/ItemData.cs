using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Items/Item")]
public class ItemData : ScriptableObject
{
    [Header("Info")]
    public string itemName;
    [TextArea] public string description;
    public Sprite icon;

    public enum ItemRarity { Common, Uncommon, Rare, Epic, Legendary }
    public ItemRarity rarity;

    [Header("Effect")]
    public BuffData.BuffType buffType;  // which stat to affect
    public float value;                 // e.g. 0.05f for +5%

    [Header("ID")]
    public string internalID;           // e.g. "smallcharm_damage"
}
