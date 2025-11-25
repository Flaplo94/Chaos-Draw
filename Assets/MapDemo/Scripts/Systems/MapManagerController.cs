using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MapManagerController : MonoBehaviour
{
    [SerializeField] private MapManager mapManager;
    [SerializeField] private Button regenerateButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private TMP_InputField seedInput;
    [SerializeField] private Toggle labelsToggle;
    [SerializeField] private Toggle dimmingToggle;
    [SerializeField] private TMP_Dropdown encounterModeDropdown;

    private void Awake()
    {
        if (regenerateButton != null)
            regenerateButton.onClick.AddListener(OnRegenerateClicked);

        if (resetButton != null)
            resetButton.onClick.AddListener(OnResetClicked);

        if (labelsToggle != null)
            labelsToggle.onValueChanged.AddListener(OnLabelsToggleChanged);

        if (dimmingToggle != null)
            dimmingToggle.onValueChanged.AddListener(OnDimmingToggleChanged);

        if (encounterModeDropdown != null)
            encounterModeDropdown.onValueChanged.AddListener(OnEncounterModeChanged);
    }

    private void OnRegenerateClicked()
    {
        if (mapManager == null) return;

        int seedValue;
        if (seedInput != null &&
            !string.IsNullOrWhiteSpace(seedInput.text) &&
            int.TryParse(seedInput.text, out seedValue))
        {
            mapManager.SetSeed(seedValue);
        }
        else
        {
            mapManager.UseRandomSeed();
        }

        mapManager.RegenerateMap();

        // Re-apply toggles after regeneration
        if (labelsToggle != null)
            mapManager.SetLabelsEnabled(labelsToggle.isOn);

        if (dimmingToggle != null)
            mapManager.SetDimmingEnabled(dimmingToggle.isOn);
    }

    private void OnResetClicked()
    {
        if (mapManager == null) return;

        mapManager.ResetPath();

        // Re-apply dimming after reset
        if (dimmingToggle != null)
            mapManager.SetDimmingEnabled(dimmingToggle.isOn);
    }

    private void OnLabelsToggleChanged(bool value)
    {
        if (mapManager == null) return;
        mapManager.SetLabelsEnabled(value);
    }

    private void OnDimmingToggleChanged(bool value)
    {
        if (mapManager == null) return;
        mapManager.SetDimmingEnabled(value);
    }

    private void OnEncounterModeChanged(int index)
    {
        if (mapManager != null)
            mapManager.SetEncounterMode(index);
    }
}
