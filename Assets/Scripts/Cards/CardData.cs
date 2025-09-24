using UnityEngine;

[CreateAssetMenu(fileName = "Card_", menuName = "ChaosDraw/Card")]
public class CardData : ScriptableObject
{
    [Header("Gameplay source (single truth)")]
    public Ability ability;   // Drag din Ability-SO her (fx Fireball, LightningRod)

    [Header("Optional overrides (utfyld KUN hvis forskelligt fra Ability)")]
    public bool overrideDisplayName; public string displayNameOverride;
    public bool overrideDescription; [TextArea] public string descriptionOverride;
    public bool overrideIcon; public Sprite iconOverride;
    public bool overrideRarity; public Rarity rarityOverride;
    public bool overrideManaCost; public float manaCostOverride;   // float matcher din Ability
    public bool overrideElement; public MagicType elementOverride;

    // --------- Effective values (brug disse i UI/logic) ----------
    public string DisplayName => overrideDisplayName ? displayNameOverride : (ability ? ability.abilityName : "");
    public string Description => overrideDescription ? descriptionOverride : (ability ? ability.description : "");
    public Sprite Icon => overrideIcon ? iconOverride : (ability ? ability.icon : null);
    public Rarity Rarity => overrideRarity ? rarityOverride : (ability ? ability.rarity : default);
    public float ManaCost => overrideManaCost ? manaCostOverride : (ability ? ability.manaCost : 0f);
    public MagicType Element => overrideElement ? elementOverride : (ability ? ability.magicType : default);
}
