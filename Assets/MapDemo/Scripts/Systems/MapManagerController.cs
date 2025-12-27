using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controller for MapManager UI.
/// Håndterer knapper, toggles og inputfelter i editor-UI'et og videresender handlinger til MapManager.
/// Kommenteret på dansk for bedre vedligeholdelse og forståelse.
/// </summary>
public class MapManagerController : MonoBehaviour
{
    [Header("Referencer")]
    [SerializeField] private MapManager mapManager;                // Reference til hoved MapManager (skal sættes i Inspector)
    [SerializeField] private Button regenerateButton;              // Knappen der triggere regeneration
    [SerializeField] private Button resetButton;                   // Knappen der nulstiller sti (Reset Path)
    [SerializeField] private TMP_InputField seedInput;             // Inputfelt til seed (valgfrit)
    [SerializeField] private Toggle labelsToggle;                  // Toggle til at vise/ skjule labels på noder
    [SerializeField] private Toggle dimmingToggle;                 // Toggle til at slå dimming af/på for kanter
    [SerializeField] private TMP_Dropdown encounterModeDropdown;   // Dropdown for valg af encounter-generation mode

    // For OptionB: foruddefinerede seeds som vælges tilfældigt hvis brugeren ikke angiver et seed
    private static readonly int[] OptionBSeeds = { 111, 222, 333 };

    /// <summary>
    /// Hook UI-events til lokale handlers ved Awake.
    /// Sikkerhedstjekper null før tilknytning.
    /// </summary>
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

    /// <summary>
    /// Håndterer klik på "Regenerate" knappen.
    /// Læser UI-state (mode + seed) og konfigurerer MapManager før regeneration.
    /// </summary>
    private void OnRegenerateClicked()
    {
        if (mapManager == null) return;

        // Læs valgt mode som label (ikke index)
        string selectedLabel = GetSelectedModeLabel();

        // Parse seed hvis angivet
        int parsed = 0;
        bool hasValidSeed = (seedInput != null) && int.TryParse(seedInput.text, out parsed);

        // Option B har særlige seed-regler: brug angivet seed, ellers vælg et forudbestemt
        if (selectedLabel == "Option B")
        {
            if (hasValidSeed)
            {
                mapManager.SetFixedSeed(parsed);
            }
            else
            {
                int chosen = OptionBSeeds[Random.Range(0, OptionBSeeds.Length)];
                mapManager.SetFixedSeed(chosen);
                // Seed input feltet skal forblive tomt (som du ønskede)
            }
        }
        else
        {
            // For andre modes: brug angivet seed eller deaktiver fixed seed
            if (hasValidSeed)
                mapManager.SetFixedSeed(parsed);
            else
                mapManager.DisableFixedSeed();
        }

        // Udfør regeneration
        mapManager.RegenerateMap();

        // Opdater visuelle indstillinger på MapManager fra toggles
        if (labelsToggle != null)
            mapManager.SetLabelsEnabled(labelsToggle.isOn);

        if (dimmingToggle != null)
            mapManager.SetDimmingEnabled(dimmingToggle.isOn);
    }

    /// <summary>
    /// Håndterer klik på "Reset" knappen.
    /// Kalder ResetPath på MapManager og genanvender dimming-indstillingen.
    /// </summary>
    private void OnResetClicked()
    {
        if (mapManager == null) return;

        mapManager.ResetPath();

        if (dimmingToggle != null)
            mapManager.SetDimmingEnabled(dimmingToggle.isOn);
    }

    /// <summary>
    /// Callback når label-toggle ændres. Videregiver direkte til MapManager.
    /// </summary>
    private void OnLabelsToggleChanged(bool value)
    {
        if (mapManager == null) return;
        mapManager.SetLabelsEnabled(value);
    }

    /// <summary>
    /// Callback når dimming-toggle ændres. Videregiver direkte til MapManager.
    /// </summary>
    private void OnDimmingToggleChanged(bool value)
    {
        if (mapManager == null) return;
        mapManager.SetDimmingEnabled(value);
    }

    /// <summary>
    /// Callback når encounter mode dropdown ændres.
    /// Mapper dropdown label -> korrekt enum værdi, så index-rækkefølge ikke kan ødelægge logik.
    /// </summary>
    private void OnEncounterModeChanged(int index)
    {
        if (mapManager == null) return;

        string label = GetSelectedModeLabel();

        // IMPORTANT: Sørg for at dropdown-teksterne matcher disse navne:
        // "Option A", "Option B", "Option C", "Option D"
        switch (label)
        {
            case "Option A":
                mapManager.SetEncounterMode(MapManager.EncounterGenerationMode.OptionA);
                break;

            case "Option B":
                mapManager.SetEncounterMode(MapManager.EncounterGenerationMode.OptionB);
                break;

            case "Option C":
                mapManager.SetEncounterMode(MapManager.EncounterGenerationMode.OptionC);
                break;

            case "Option D":
                mapManager.SetEncounterMode(MapManager.EncounterGenerationMode.OptionD);
                break;

            default:
                // Fallback hvis dropdown-tekst ikke matcher (sikkerhed)
                mapManager.SetEncounterMode(MapManager.EncounterGenerationMode.OptionA);
                break;
        }
    }

    /// <summary>
    /// Hjælper: returnerer teksten på den valgte dropdown-option.
    /// </summary>
    private string GetSelectedModeLabel()
    {
        if (encounterModeDropdown == null) return "Option A";

        int idx = encounterModeDropdown.value;
        if (idx < 0 || idx >= encounterModeDropdown.options.Count) return "Option A";

        return encounterModeDropdown.options[idx].text;
    }
}
