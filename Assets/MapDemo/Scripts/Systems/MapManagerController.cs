using UnityEngine;
using UnityEngine.UI;

public class MapManagerController : MonoBehaviour
{
    [SerializeField] private MapManager mapManager;
    [SerializeField] private Button regenerateButton;
    [SerializeField] private Button resetButton;

    private void Awake()
    {
        if (regenerateButton != null)
            regenerateButton.onClick.AddListener(mapManager.RegenerateMap);

        if (resetButton != null)
            resetButton.onClick.AddListener(mapManager.ResetPath);
    }

    private void Start()
    {
        // First click makes a map, but it's convenient to auto-generate on load too.
        // If you prefer first-click-only, delete this line.
        // mapManager.RegenerateMap();
    }
}
