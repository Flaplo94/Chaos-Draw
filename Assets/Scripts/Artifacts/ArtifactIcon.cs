using UnityEngine;
using UnityEngine.UI;

public class ArtifactIcon : MonoBehaviour
{
    public Image iconImage;

    public void Setup(ArtifactData data)
    {
        if (data != null && iconImage != null)
            iconImage.sprite = data.icon;
    }
}
