using UnityEngine;
using System.Collections.Generic;

public class AbilitySystem : MonoBehaviour
{
    public static AbilitySystem Instance;

    private List<CardData> abilities = new List<CardData>();
    private PlayerAbilities playerAbilities;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        playerAbilities = FindObjectOfType<PlayerAbilities>();
        if (playerAbilities == null)
        {
            Debug.LogError("PlayerAbilities not found in scene.");
        }
    }

    public void AddAbility(CardData abilityCard)
    {
        abilities.Add(abilityCard);
        Debug.Log("Ability added: " + abilityCard.cardName);

        // Send videre til PlayerAbilities
        if (playerAbilities != null)
        {
            playerAbilities.AddAbility(abilityCard);
        }
    }

    public List<CardData> GetAbilities()
    {
        return abilities;
    }
}
