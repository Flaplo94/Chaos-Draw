using UnityEngine;
using System.Collections.Generic;

public class BuffSystem : MonoBehaviour
{
    public static BuffSystem Instance;

    private Dictionary<string, int> activeBuffs = new Dictionary<string, int>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void ApplyBuff(string buffID)
    {
        if (activeBuffs.ContainsKey(buffID))
            activeBuffs[buffID]++;
        else
            activeBuffs.Add(buffID, 1);

        Debug.Log("Buff applied: " + buffID + " (level " + activeBuffs[buffID] + ")");
    }

    public int GetBuffLevel(string buffID)
    {
        return activeBuffs.ContainsKey(buffID) ? activeBuffs[buffID] : 0;
    }
}
