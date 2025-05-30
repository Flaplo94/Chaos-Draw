using UnityEngine;

public enum CardType { Buff, Ability }
public enum CardRarity { Common, Uncommon, Rare, Epic, Legendary }

[CreateAssetMenu(fileName = "NewCard", menuName = "Card")]
public class CardData : ScriptableObject
{
    public string cardName;
    [TextArea] public string description;
    public Sprite icon;
    public CardType cardType;
    public CardRarity rarity;
    public GameObject cardEffectPrefab;
}
