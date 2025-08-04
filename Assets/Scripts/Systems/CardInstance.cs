using UnityEngine;

[System.Serializable]
public class CardInstance
{
    public Ability ability;

    public CardInstance(Ability ability)
    {
        this.ability = ability;
        Debug.Log("Created card: " + ability.abilityName + ", prefab: " + (ability.effectPrefab != null));
    }
}