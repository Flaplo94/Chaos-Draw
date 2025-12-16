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

    // Option B preset seeds
    private static readonly int[] OptionBSeeds = { 111, 222, 333 };

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

        int modeIndex = (encounterModeDropdown != null) ? encounterModeDropdown.value : 0;
        int parsed = 0;
        bool hasValidSeed = (seedInput != null) && int.TryParse(seedInput.text, out parsed);

        // Option B special behavior: if no seed entered, force one of {111,222,333}
        // Assumption from our setup: dropdown index 1 == Option B (A=0, B=1, C=2, ...)
        if (modeIndex == 1)
        {
            if (hasValidSeed)
            {
                mapManager.SetFixedSeed(parsed);
            }
            else
            {
                int chosen = OptionBSeeds[Random.Range(0, OptionBSeeds.Length)];
                mapManager.SetFixedSeed(chosen);
            }
        }
        else
        {
            // Option A/C keep current behavior
            if (hasValidSeed)
                mapManager.SetFixedSeed(parsed);
            else
                mapManager.DisableFixedSeed();
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
