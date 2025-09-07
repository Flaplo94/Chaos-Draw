using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardRewardUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text nameText;         // AbilityName
    public Image artImage;             // AbilityArt/Icon
    public Image rarityIcon;           // RarityIcon
    public TMP_Text dmgText;           // StatsRow/DamageIcon/DamageValue
    public TMP_Text manaText;          // StatsRow/ManaIcon/ManaValue
    public TMP_Text descriptionText;   // Description

    public void Setup(Ability ability, Sprite raritySprite = null)
    {
        if (!ability) return;

        if (nameText) nameText.text = ability.abilityName;

        if (artImage)
        {
            artImage.sprite = ability.icon;
            artImage.color = ability.icon ? Color.white : Color.clear;
            artImage.preserveAspect = true;
        }

        if (rarityIcon && raritySprite != null)
        {
            rarityIcon.enabled = true;
            rarityIcon.sprite = raritySprite;
        }

        if (dmgText) dmgText.text = ability.damage > 0 ? ability.damage.ToString() : "—";
        if (manaText) manaText.text = ability.manaCost.ToString("0");
        if (descriptionText) descriptionText.text = ability.description;
    }
}
