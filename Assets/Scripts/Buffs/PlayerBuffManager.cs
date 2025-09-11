using System.Collections.Generic;
using UnityEngine;

public class PlayerBuffManager : MonoBehaviour
{
    public static PlayerBuffManager Instance;
    public event System.Action OnValuesChanged;

    [Header("Damage")]
    [SerializeField] private float genericDamageMult = 1f;
    [SerializeField] private float fireDamageMult = 1f;
    [SerializeField] private float thunderDamageMult = 1f;
    [SerializeField] private float burnDamageMult = 1f;
    [SerializeField] private float scalingDamageMult = 1f;

    [Header("Core Stats")]
    [SerializeField] private float maxHpMult = 1f;
    [SerializeField] private float shieldRegenMult = 1f;
    [SerializeField] private float manaRegenMult = 1f;
    [SerializeField] private float manaCostReductionMult = 1f;

    [Header("Combat")]
    [SerializeField] private float attackSpeedMult = 1f;
    [SerializeField] private int extraProjectiles = 0;
    [SerializeField] private float lifestealMult = 0f;

    [Header("Utility")]
    [SerializeField] private float moveSpeedMult = 1f;
    [SerializeField] private float goldGainMult = 1f;
    [SerializeField] private float chaosShardGainMult = 1f;

    private readonly List<ActiveBuff> activeBuffs = new();
    public IReadOnlyList<ActiveBuff> ActiveBuffs => activeBuffs;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            activeBuffs[i].timeLeft -= Time.deltaTime;
            if (activeBuffs[i].timeLeft <= 0)
            {
                RemoveBuff(activeBuffs[i].data);
                activeBuffs.RemoveAt(i);
            }
        }
    }

    public void ApplyBuff(BuffData data)
    {
        ModifyBuffValue(data, add: true);

        if (data.duration > 0)
            activeBuffs.Add(new ActiveBuff(data, data.duration));
    }

    public void RemoveBuff(BuffData data)
    {
        ModifyBuffValue(data, add: false);
    }

    public void AddRuntimeBonus(BuffData.BuffType type, float value)
    {
        BuffData temp = ScriptableObject.CreateInstance<BuffData>();
        temp.type = type;
        temp.value = value;
        temp.duration = 0f;
        ApplyBuff(temp);
    }

    private void ModifyBuffValue(BuffData data, bool add)
    {
        float sign = add ? 1f : -1f;
        switch (data.type)
        {
            case BuffData.BuffType.Damage: genericDamageMult += sign * data.value; break;
            case BuffData.BuffType.FireDamage: fireDamageMult += sign * data.value; break;
            case BuffData.BuffType.ThunderDamage: thunderDamageMult += sign * data.value; break;
            case BuffData.BuffType.BurnDamage: burnDamageMult += sign * data.value; break;
            case BuffData.BuffType.ScalingDamage: scalingDamageMult += sign * data.value; break;

            case BuffData.BuffType.MaxHP: maxHpMult += sign * data.value; break;
            case BuffData.BuffType.ShieldRegen: shieldRegenMult += sign * data.value; break;
            case BuffData.BuffType.ManaRegen: manaRegenMult += sign * data.value; break;
            case BuffData.BuffType.ManaCostReduction: manaCostReductionMult += sign * data.value; break;

            case BuffData.BuffType.AttackSpeed: attackSpeedMult += sign * data.value; break;
            case BuffData.BuffType.ExtraProjectile: extraProjectiles += Mathf.RoundToInt(sign * data.value); break;
            case BuffData.BuffType.Lifesteal: lifestealMult += sign * data.value; break;

            case BuffData.BuffType.Speed: moveSpeedMult += sign * data.value; break;
            case BuffData.BuffType.GoldGain: goldGainMult += sign * data.value; break;
            case BuffData.BuffType.ChaosShardGain: chaosShardGainMult += sign * data.value; break;
        }
        OnValuesChanged?.Invoke();
    }

    // --- Getters (til andre systemer) ---
    public float GetGenericDamageMult() => genericDamageMult;
    public float GetFireDamageMult() => fireDamageMult;
    public float GetThunderDamageMult() => thunderDamageMult;
    public float GetBurnDamageMult() => burnDamageMult;
    public float GetScalingDamageMult() => scalingDamageMult;

    public float GetMoveSpeedMult() => moveSpeedMult;
    public float GetAttackSpeedMult() => attackSpeedMult;
    public float GetGoldGainMult() => goldGainMult;
    public float GetChaosShardGainMult() => chaosShardGainMult;

    public int GetExtraProjectiles() => extraProjectiles;
    public float GetLifestealMult() => lifestealMult;
    public float GetMaxHpMult() => maxHpMult;
    public float GetShieldRegenMult() => shieldRegenMult;
    public float GetManaRegenMult() => manaRegenMult;
    public float GetManaCostReductionMult() => manaCostReductionMult;

    public class ActiveBuff
    {
        public BuffData data;
        public float timeLeft;

        public ActiveBuff(BuffData data, float duration)
        {
            this.data = data;
            this.timeLeft = duration;
        }
    }

}
