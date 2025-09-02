using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardRewardUI : MonoBehaviour
{
    [Header("Main")]
    public TextMeshProUGUI nameText;
    public Image artImage;
    public TextMeshProUGUI rarityText;

    [Header("Stats")]
    public TextMeshProUGUI dmgText;
    public TextMeshProUGUI manaText;

    [Header("Tags")]
    public Transform tagRow;
}
