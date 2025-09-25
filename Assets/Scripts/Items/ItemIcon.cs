using UnityEngine;
using UnityEngine.UI;

public class ItemIcon : MonoBehaviour
{
    public Image iconImage;

    public void Setup(ItemData data)
    {
        if (data != null && iconImage != null)
            iconImage.sprite = data.icon;
    }
}
