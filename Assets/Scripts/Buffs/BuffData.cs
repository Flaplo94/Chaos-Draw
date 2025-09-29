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
    public float value = 0f;     // procent (som faktor) eller rå værdi (HPRegen)
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
        Armor,     // NEW: incoming damage reduction (0.10 = 10% mindre skade)
        HPRegen,   // NEW: HP pr. 5 sek (rå mængde)

        // Mana / Magic
        ManaRegen,
        ManaCostReduction,
        ManaCostFlat,        // flat reduction in mana cost

        // Offense
        AttackSpeed,
        ExtraProjectile,
        Lifesteal,

        // Utility
        Speed,
        GoldGain,
        ChaosShardGain
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
