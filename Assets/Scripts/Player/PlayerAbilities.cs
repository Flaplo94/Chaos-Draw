using UnityEngine;
using System.Collections.Generic;

public class PlayerAbilities : MonoBehaviour
{
    private List<CardData> equippedAbilities = new List<CardData>();

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) UseAbility(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) UseAbility(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) UseAbility(2);
    }

    public void AddAbility(CardData abilityCard)
    {
        if (equippedAbilities.Count < 3)
        {
            equippedAbilities.Add(abilityCard);
            Debug.Log("Added ability: " + abilityCard.cardName);
        }
        else
        {
            // Opgrader eller udskift logik her
            Debug.Log("All ability slots are full.");
        }
    }

    private void UseAbility(int index)
    {
        if (index >= equippedAbilities.Count) return;

        string name = equippedAbilities[index].cardName;

        switch (name)
        {
            case "Dash":
                // fx: GetComponent<PlayerMovement>().Dash();
                Debug.Log("DASH used!");
                break;
            case "Shield":
                // fx: GetComponent<PlayerHealth>().ActivateShield();
                Debug.Log("SHIELD used!");
                break;
            case "Fireball":
                // fx: SpawnFireball();
                Debug.Log("FIREBALL used!");
                break;
            default:
                Debug.Log("No ability effect for " + name);
                break;
        }
    }
}
