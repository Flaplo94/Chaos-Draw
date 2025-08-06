using UnityEngine;
using UnityEngine.UI;

public class BuffIcon : MonoBehaviour
{
    [Header("UI Elements")]
    public Image iconImage;

    // Kaldes af BuffUIManager når ikonet skal vises
    public void Setup(BuffData buffData)
    {
        if (iconImage != null && buffData.icon != null)
            iconImage.sprite = buffData.icon;
        else
            Debug.LogWarning("BuffIcon: Mangler iconImage eller buffData.icon");
    }
}
