using UnityEngine;

public enum CardType { Buff, Ability }
public enum CardRarity { Common, Uncommon, Rare, Epic, Legendary }

[CreateAssetMenu(fileName = "New Card", menuName = "Cards/Card Data")]
public class CardData : ScriptableObject
{
    public string cardName;
    public string description;
    public Sprite cardSprite;
    public CardType cardType;
    public CardRarity rarity;
}
