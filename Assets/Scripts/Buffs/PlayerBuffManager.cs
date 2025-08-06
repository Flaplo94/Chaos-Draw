using System.Collections.Generic;
using UnityEngine;

public class PlayerBuffManager : MonoBehaviour
{
    public static PlayerBuffManager Instance;

    public List<BuffData> activeBuffs = new();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void AddBuff(BuffData buff)
    {
        activeBuffs.Add(buff);
        Debug.Log($"Buff added: {buff.buffName}");

        // Opdater UI
        BuffUIManager ui = FindFirstObjectByType<BuffUIManager>();
        if (ui != null)
            ui.UpdateBuffUI();
    }

    public float GetTotalValue(BuffData.BuffType type)
    {
        float total = 0f;
        foreach (var buff in activeBuffs)
        {
            if (buff.type == type)
                total += buff.value;
        }
        return total;
    }

    public bool HasBuff(BuffData.BuffType type)
    {
        return activeBuffs.Exists(b => b.type == type);
    }
}
