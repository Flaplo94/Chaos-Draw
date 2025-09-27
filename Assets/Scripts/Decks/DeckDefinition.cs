using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ChaosDraw/Deck Definition")]
public class DeckDefinition : ScriptableObject
{
    [Header("Identity")]
    public string id;                // fx "fire", "lightning"
    public string deckName;          // "Fire Deck"
    [TextArea] public string tagline;

    [Header("UI")]
    public Sprite icon;              // brug card back
    public Sprite banner;            // kan være samme som icon
    public Color uiTint = Color.white;

    [Header("Unlock")]
    public bool unlockedByDefault = false;

    [Header("Starting Cards (preview only)")]
    public List<CardData> startingCardsPreview = new List<CardData>();
}
