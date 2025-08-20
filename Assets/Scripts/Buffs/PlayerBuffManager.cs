using System.Collections.Generic;
using UnityEngine;

public class PlayerBuffManager : MonoBehaviour
{
    public static PlayerBuffManager Instance;

    [Header("Active Buff Assets (from cards/shop)")]
    public List<BuffData> activeBuffs = new List<BuffData>();

    // Runtime additive bonuses per BuffType (used by artifacts)
    private readonly Dictionary<BuffData.BuffType, float> runtimeAdd =
        new Dictionary<BuffData.BuffType, float>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // ----- Mutations -----

    public void AddBuff(BuffData buff)
    {
        if (buff == null) return;
        activeBuffs.Add(buff);
        Debug.Log($"Buff added: {buff.buffName}");

        var ui = FindFirstObjectByType<BuffUIManager>();
        if (ui != null) ui.UpdateBuffUI();
    }

    public void AddRuntimeBonus(BuffData.BuffType type, float value)
    {
        if (!runtimeAdd.ContainsKey(type)) runtimeAdd[type] = 0f;
        runtimeAdd[type] += value;
    }

    // ----- Queries -----

    public float GetTotalValue(BuffData.BuffType type)
    {
        float total = 0f;
        for (int i = 0; i < activeBuffs.Count; i++)
        {
            var b = activeBuffs[i];
            if (b != null && b.type == type) total += b.value;
        }
        float r;
        if (runtimeAdd.TryGetValue(type, out r)) total += r;
        return total;
    }

    public bool HasBuff(BuffData.BuffType type)
    {
        if (GetTotalValue(type) != 0f) return true;
        for (int i = 0; i < activeBuffs.Count; i++)
            if (activeBuffs[i] != null && activeBuffs[i].type == type) return true;
        return false;
    }

    // ----- Convenience getters -----

    // Movement
    public float GetMoveSpeedMult() => 1f + GetTotalValue(BuffData.BuffType.Speed);

    // Attack rate
    public float GetAttackSpeedMult() => 1f + GetTotalValue(BuffData.BuffType.AttackSpeed);

    // Projectiles
    public int GetExtraProjectiles() => Mathf.RoundToInt(GetTotalValue(BuffData.BuffType.ExtraProjectile));

    // HP
    public float GetMaxHpMult() => 1f + GetTotalValue(BuffData.BuffType.MaxHP);
    public float GetLifestealFraction() => Mathf.Max(0f, GetTotalValue(BuffData.BuffType.Lifesteal));

    // Resources
    public int GetManaCostReduction() => Mathf.RoundToInt(GetTotalValue(BuffData.BuffType.ManaCostReduction));
    public float GetGoldGainMult() => 1f + GetTotalValue(BuffData.BuffType.GoldGain);

    // Elements/status
    public float GetFireDamageMult() => 1f + GetTotalValue(BuffData.BuffType.FireDamage);
    public float GetLightningDamageMult() => 1f + GetTotalValue(BuffData.BuffType.ThunderDamage);
    public float GetBurnDamageMult() => 1f + GetTotalValue(BuffData.BuffType.BurnDamage);

    // Generic damage including ScalingDamage (Adaptive Power)
    public float GetGenericDamageMult()
    {
        float generic = 1f + GetTotalValue(BuffData.BuffType.Damage);
        float perBuff = GetTotalValue(BuffData.BuffType.ScalingDamage);
        int buffCount = activeBuffs.Count; // only ScriptableObject buffs count here
        float scaling = 1f + Mathf.Max(0f, perBuff) * Mathf.Max(0, buffCount);
        return generic * scaling;
    }
}
