using UnityEngine;
using System.Collections.Generic;

public class AbilitySystem : MonoBehaviour
{
    public static AbilitySystem Instance;

    private List<CardData> abilities = new List<CardData>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void AddAbility(CardData abilityCard)
    {
        abilities.Add(abilityCard);
        Debug.Log("Ability added: " + abilityCard.cardName);
    }

    public List<CardData> GetAbilities()
    {
        return abilities;
    }
}
