using UnityEngine;
using UnityEngine.UI;

public class MapNodeButton : MonoBehaviour
{
    public int nodeId;
    public Button button;
    public Image background;

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        if (background == null) background = GetComponent<Image>();
    }

    public void SetInteractable(bool interactable)
    {
        button.interactable = interactable;
        if (background != null)
            background.color = interactable ? Color.white : new Color(0.6f, 0.6f, 0.6f, 1f);
    }

    public void SetSelected()
    {
        if (background != null)
            background.color = Color.green;
    }
}
