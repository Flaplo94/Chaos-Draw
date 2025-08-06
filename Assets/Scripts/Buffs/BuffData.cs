using UnityEngine;

[CreateAssetMenu(fileName = "New Buff", menuName = "Buffs/Buff")]
public class BuffData : ScriptableObject
{
    public string buffName;
    [TextArea] public string description;
    public Sprite icon;

    public enum BuffType
    {
        Damage,
        FireDamage,
        ThunderDamage,
        BurnDamage,
        MaxHP,
        ManaRegen,
        AttackSpeed,
        Speed,
        GoldGain,
        Lifesteal,
        ManaCostReduction,
        XP,
        ShieldRegen,
        ExtraProjectile,
        ScalingDamage
    }

    public BuffType type;
    public float value;
    public enum BuffRarity { Common, Uncommon, Rare, Epic, Legendary }
    public BuffRarity rarity;
}
