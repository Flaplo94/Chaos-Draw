using UnityEngine;

[CreateAssetMenu(fileName = "New Buff", menuName = "Buffs/Buff")]
public class BuffData : ScriptableObject
{
    [Header("Basic Info")]
    public string buffName;
    [TextArea] public string description;
    public Sprite icon;

    [Header("Core Settings")]
    public BuffType type;        // hvilken stat buffen rammer
    public float value = 0f;     // procent eller flat værdi (afhænger af buff)
    public float duration = 0f;  // 0 = permanent, >0 = tidsbegrænset buff

    [Header("Rarity")]
    public BuffRarity rarity = BuffRarity.Common;

    public enum BuffType
    {
        // Damage
        Damage,
        FireDamage,
        ThunderDamage,
        BurnDamage,
        ScalingDamage,

        // Survivability
        MaxHP,
        ShieldRegen,

        // Mana / Magic
        ManaRegen,
        ManaCostReduction,

        // Offense
        AttackSpeed,
        ExtraProjectile,
        Lifesteal,

        // Utility
        Speed,
        GoldGain,
        ChaosShardGain   // <- ændret fra XP til ChaosShardGain
    }

    public enum BuffRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }
}
