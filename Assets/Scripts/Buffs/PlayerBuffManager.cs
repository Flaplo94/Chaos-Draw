using System.Collections.Generic;
using UnityEngine;

public class PlayerBuffManager : MonoBehaviour
{
    public static PlayerBuffManager Instance;
    public event System.Action OnValuesChanged;

    // --- Persist keys ---
    const string K_DAMAGE = "PBM_damage";
    const string K_FIRE = "PBM_fire";
    const string K_THUNDER = "PBM_thunder";
    const string K_BURN = "PBM_burn";
    const string K_SCALING = "PBM_scaling";
    const string K_MAXHP = "PBM_maxhp";
    const string K_SHIELDREGEN = "PBM_shieldregen";
    const string K_ARMOR = "PBM_armor_pct";
    const string K_HPREGEN = "PBM_hpregen5";
    const string K_ATKSPD = "PBM_atkspd";
    const string K_EXTRAP = "PBM_extraprojs";
    const string K_LIFESTEAL = "PBM_lifesteal";
    const string K_MOVESPD = "PBM_movespd";
    const string K_GOLD = "PBM_gold";
    const string K_SHARDS = "PBM_shards";
    const string K_MANAREGEN = "PBM_manaregen";
    const string K_MANACOST = "PBM_manacost";

    [Header("Damage")]
    [SerializeField] private float genericDamageMult = 1f;
    [SerializeField] private float fireDamageMult = 1f;
    [SerializeField] private float thunderDamageMult = 1f;
    [SerializeField] private float burnDamageMult = 1f;
    [SerializeField] private float scalingDamageMult = 1f;

    [Header("Core Stats")]
    [SerializeField] private float maxHpMult = 1f;
    [SerializeField] private float shieldRegenMult = 1f;

    [Header("Survivability")]
    [SerializeField, Tooltip("Additiv procent reduktion af indgående skade. 0.10 = 10% mindre damage.")]
    private float armorReductionPct = 0f;
    [SerializeField, Tooltip("HP der heales hvert 5. sekund (afrundes til heltal).")]
    private float hpRegenPer5Sec = 0f;

    [Header("Mana / Magic")]
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
        else { Destroy(gameObject); return; }

        // Load persisted totals so stats survive scene changes / play sessions
        LoadPersistentTotals();
        OnValuesChanged?.Invoke();
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
        var temp = ScriptableObject.CreateInstance<BuffData>();
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
            // Damage
            case BuffData.BuffType.Damage: genericDamageMult += sign * data.value; break;
            case BuffData.BuffType.FireDamage: fireDamageMult += sign * data.value; break;
            case BuffData.BuffType.ThunderDamage: thunderDamageMult += sign * data.value; break;
            case BuffData.BuffType.BurnDamage: burnDamageMult += sign * data.value; break;
            case BuffData.BuffType.ScalingDamage: scalingDamageMult += sign * data.value; break;

            // Survivability
            case BuffData.BuffType.MaxHP: maxHpMult += sign * data.value; break;
            case BuffData.BuffType.ShieldRegen: shieldRegenMult += sign * data.value; break;
            case BuffData.BuffType.Armor: armorReductionPct += sign * data.value; break;
            case BuffData.BuffType.HPRegen: hpRegenPer5Sec += sign * data.value; break;

            // Mana / Magic
            case BuffData.BuffType.ManaRegen: manaRegenMult += sign * data.value; break;
            case BuffData.BuffType.ManaCostReduction: manaCostReductionMult += sign * data.value; break;

            // Combat
            case BuffData.BuffType.AttackSpeed: attackSpeedMult += sign * data.value; break;
            case BuffData.BuffType.ExtraProjectile: extraProjectiles += Mathf.RoundToInt(sign * data.value); break;
            case BuffData.BuffType.Lifesteal: lifestealMult += sign * data.value; break;

            // Utility
            case BuffData.BuffType.Speed: moveSpeedMult += sign * data.value; break;
            case BuffData.BuffType.GoldGain: goldGainMult += sign * data.value; break;
            case BuffData.BuffType.ChaosShardGain: chaosShardGainMult += sign * data.value; break;
        }
        OnValuesChanged?.Invoke();
        // Bemærk: Vi saver IKKE automatisk her – NodeEffects gør det efter skill-køb,
        // så midlertidige buffs ikke bliver persisteret ved et uheld.
    }

    // --- Getters (til andre systemer/Stat UI) ---
    public float GetGenericDamageMult() => genericDamageMult;
    public float GetFireDamageMult() => fireDamageMult;
    public float GetThunderDamageMult() => thunderDamageMult;
    public float GetBurnDamageMult() => burnDamageMult;
    public float GetScalingDamageMult() => scalingDamageMult;

    public float GetMaxHpMult() => maxHpMult;
    public float GetShieldRegenMult() => shieldRegenMult;
    public float GetArmorReductionPct() => armorReductionPct;
    public float GetHPRegenPer5Sec() => hpRegenPer5Sec;

    public float GetMoveSpeedMult() => moveSpeedMult;
    public float GetAttackSpeedMult() => attackSpeedMult;
    public float GetGoldGainMult() => goldGainMult;
    public float GetChaosShardGainMult() => chaosShardGainMult;

    public int GetExtraProjectiles() => extraProjectiles;
    public float GetLifestealMult() => lifestealMult;

    public float GetManaRegenMult() => manaRegenMult;
    public float GetManaCostReductionMult() => manaCostReductionMult;

    // --- Persistence API (kaldes fra NodeEffects + på Awake) ---
    public void SavePersistentTotals()
    {
        PlayerPrefs.SetFloat(K_DAMAGE, genericDamageMult);
        PlayerPrefs.SetFloat(K_FIRE, fireDamageMult);
        PlayerPrefs.SetFloat(K_THUNDER, thunderDamageMult);
        PlayerPrefs.SetFloat(K_BURN, burnDamageMult);
        PlayerPrefs.SetFloat(K_SCALING, scalingDamageMult);

        PlayerPrefs.SetFloat(K_MAXHP, maxHpMult);
        PlayerPrefs.SetFloat(K_SHIELDREGEN, shieldRegenMult);
        PlayerPrefs.SetFloat(K_ARMOR, armorReductionPct);
        PlayerPrefs.SetFloat(K_HPREGEN, hpRegenPer5Sec);

        PlayerPrefs.SetFloat(K_ATKSPD, attackSpeedMult);
        PlayerPrefs.SetInt(K_EXTRAP, extraProjectiles);
        PlayerPrefs.SetFloat(K_LIFESTEAL, lifestealMult);

        PlayerPrefs.SetFloat(K_MOVESPD, moveSpeedMult);
        PlayerPrefs.SetFloat(K_GOLD, goldGainMult);
        PlayerPrefs.SetFloat(K_SHARDS, chaosShardGainMult);

        PlayerPrefs.SetFloat(K_MANAREGEN, manaRegenMult);
        PlayerPrefs.SetFloat(K_MANACOST, manaCostReductionMult);

        PlayerPrefs.Save();
    }

    public void LoadPersistentTotals()
    {
        // Brug default hvis key ikke findes
        genericDamageMult = PlayerPrefs.GetFloat(K_DAMAGE, 1f);
        fireDamageMult = PlayerPrefs.GetFloat(K_FIRE, 1f);
        thunderDamageMult = PlayerPrefs.GetFloat(K_THUNDER, 1f);
        burnDamageMult = PlayerPrefs.GetFloat(K_BURN, 1f);
        scalingDamageMult = PlayerPrefs.GetFloat(K_SCALING, 1f);

        maxHpMult = PlayerPrefs.GetFloat(K_MAXHP, 1f);
        shieldRegenMult = PlayerPrefs.GetFloat(K_SHIELDREGEN, 1f);
        armorReductionPct = PlayerPrefs.GetFloat(K_ARMOR, 0f);
        hpRegenPer5Sec = PlayerPrefs.GetFloat(K_HPREGEN, 0f);

        attackSpeedMult = PlayerPrefs.GetFloat(K_ATKSPD, 1f);
        extraProjectiles = PlayerPrefs.GetInt(K_EXTRAP, 0);
        lifestealMult = PlayerPrefs.GetFloat(K_LIFESTEAL, 0f);

        moveSpeedMult = PlayerPrefs.GetFloat(K_MOVESPD, 1f);
        goldGainMult = PlayerPrefs.GetFloat(K_GOLD, 1f);
        chaosShardGainMult = PlayerPrefs.GetFloat(K_SHARDS, 1f);

        manaRegenMult = PlayerPrefs.GetFloat(K_MANAREGEN, 1f);
        manaCostReductionMult = PlayerPrefs.GetFloat(K_MANACOST, 1f);
    }

    public static void ClearPersistentTotals()
    {
        string[] keys = {
            K_DAMAGE,K_FIRE,K_THUNDER,K_BURN,K_SCALING,
            K_MAXHP,K_SHIELDREGEN,K_ARMOR,K_HPREGEN,
            K_ATKSPD,K_EXTRAP,K_LIFESTEAL,
            K_MOVESPD,K_GOLD,K_SHARDS,
            K_MANAREGEN,K_MANACOST
        };
        foreach (var k in keys) PlayerPrefs.DeleteKey(k);
        PlayerPrefs.Save();
    }

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
