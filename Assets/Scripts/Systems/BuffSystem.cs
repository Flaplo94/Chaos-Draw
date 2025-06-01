using UnityEngine;
using System.Collections.Generic;

public class BuffSystem : MonoBehaviour
{
    public static BuffSystem Instance;

    private List<CardData> activeBuffs = new List<CardData>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void AddBuff(CardData buffCard)
    {
        activeBuffs.Add(buffCard);
        Debug.Log("Buff added: " + buffCard.cardName);
    }

    public int GetBuffLevel(string buffName)
    {
        int count = 0;
        foreach (var buff in activeBuffs)
        {
            if (buff.cardName == buffName)
                count++;
        }
        return count;
    }

    public List<CardData> GetAllBuffs()
    {
        return activeBuffs;
    }
}
