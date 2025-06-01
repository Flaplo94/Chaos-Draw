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

        if (pool.Count == 0)
        {
            Debug.LogWarning("No cards available of type: " + type);
            return result;
        }

        for (int i = 0; i < count; i++)
        {
            CardRarity selectedRarity = RollRarity();
            var candidates = pool.Where(c => c.rarity == selectedRarity).ToList();

            if (candidates.Count == 0)
                candidates = pool;

            if (candidates.Count == 0)
                break;

            var selected = candidates[Random.Range(0, candidates.Count)];
            result.Add(selected);
        }

        return result;
    }

    private CardRarity RollRarity()
    {
        float roll = Random.value;

        if (roll < 0.50f) return CardRarity.Common;
        if (roll < 0.75f) return CardRarity.Uncommon;
        if (roll < 0.90f) return CardRarity.Rare;
        if (roll < 0.97f) return CardRarity.Epic;
        return CardRarity.Legendary;
    }
}
