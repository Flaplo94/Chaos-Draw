using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardSlotUI : MonoBehaviour
{
    [Header("Hook up your prefab parts")]
    [SerializeField] private Image background;          // the parent Image on the card prefab
    [SerializeField] private TextMeshProUGUI nameText;  // top TMP (ability name)
    [SerializeField] private Image iconImage;           // bottom Image (ability icon)

    [Header("Empty State")]
    [SerializeField] private Color emptyBgColor = new Color(1, 1, 1, 0);  // transparent by default
    [SerializeField] private Color emptyIconColor = new Color(1, 1, 1, 0);// hide the icon when empty

    public void Show(Ability ability, Color rarityColor)
    {
        if (background) background.color = rarityColor;
        if (nameText) nameText.text = ability.abilityName;
        if (iconImage)
        {
            iconImage.sprite = ability.icon;
            iconImage.color = Color.white;
        }
    }

    public void Clear()
    {
        if (background) background.color = emptyBgColor;
        if (nameText) nameText.text = "";
        if (iconImage)
        {
            iconImage.sprite = null;
            iconImage.color = emptyIconColor;
        }
    }
}
