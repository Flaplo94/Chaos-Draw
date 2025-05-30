using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class CardManager : MonoBehaviour
{
    public static CardManager Instance;

    public List<CardData> allCards;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public List<CardData> GetRandomCards(int count, CardType type)
    {
        List<CardData> pool = allCards.Where(card => card.cardType == type).ToList();
        List<CardData> result = new List<CardData>();

        for (int i = 0; i < count; i++)
        {
            CardRarity selectedRarity = RollRarity();

            // Prøv at finde kort i den valgte rarity
            var candidates = pool.Where(c => c.rarity == selectedRarity).ToList();

            // Hvis ingen findes, falder vi tilbage til alle kort af korrekt type
            if (candidates.Count == 0)
                candidates = pool;

            var selected = candidates[Random.Range(0, candidates.Count)];
            result.Add(selected);
        }

        return result;
    }

    private CardRarity RollRarity()
    {
        float roll = Random.value;

        if (roll < 0.50f) return CardRarity.Common;      // 50%
        if (roll < 0.75f) return CardRarity.Uncommon;    // 25%
        if (roll < 0.90f) return CardRarity.Rare;        // 15%
        if (roll < 0.97f) return CardRarity.Epic;        // 7%
        return CardRarity.Legendary;                     // 3%
    }
}
