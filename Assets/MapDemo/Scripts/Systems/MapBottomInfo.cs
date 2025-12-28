using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;

// Dette script er lavet af Marc
/// <summary>
/// MapBottomInfo
/// Ansvar: Sammensætter og viser diagnostisk / statistisk information i bunden af kort-UI.
/// - Modtager data fra MapManager og formaterer en tekststreng til en TMP_Text.
/// - Designet som en enkel view-komponent (ingen kompleks logik).
/// </summary>
public class MapBottomInfo : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private TMP_Text infoText; // Tekstkomponent hvor informationen vises

    /// <summary>
    /// Opbygger og viser bund-information for det aktuelle kort/run.
    /// Metoden er stateless og formaterer blot de indkommende parametre til tekst.
    /// </summary>
    /// <param name="generationMs">Tid brugt på at generere kortet (ms).</param>
    /// <param name="oneChoiceNodes">Antal noder med kun et valg.</param>
    /// <param name="encounterCounts">Dictionary med tællinger pr encounter-type (kan være null).</param>
    /// <param name="runCompleted">Om run/sti blev fuldført.</param>
    /// <param name="lastIntervalSeconds">Tid siden sidste klik (sekunder).</param>
    /// <param name="totalRunSeconds">Total tid for run (sekunder), kun relevant hvis runCompleted==true.</param>
    /// <param name="avgIntervalSeconds">Gennemsnitstid mellem klik (sekunder), kun relevant ved fuldført run.</param>
    /// <param name="medianIntervalSeconds">Median tid mellem klik (sekunder), kun relevant ved fuldført run.</param>
    /// <param name="optionAPass">Option A: success flag (generatorens validering).</param>
    /// <param name="optionCPass">Option C: success flag (generatorens validering).</param>
    /// <param name="optionBPass">Option B: success flag (generatorens validering).</param>
    /// <param name="selectedMode">Den valgte encounter generation mode.</param>
    public void ShowInfo(
        double generationMs,
        int oneChoiceNodes,
        Dictionary<string, int> encounterCounts,
        bool runCompleted,
        float lastIntervalSeconds,
        float totalRunSeconds,
        float avgIntervalSeconds,
        float medianIntervalSeconds,
        bool optionAPass,
        bool optionBPass,
        bool optionCPass,
        bool optionDPass,
        MapManager.EncounterGenerationMode selectedMode)
    {
        if (infoText == null)
        {
            Debug.LogWarning("MapBottomInfo: infoText er ikke tildelt i Inspector.");
            return;
        }

        var sb = new StringBuilder();

        // Generations-tid
        sb.AppendLine($"Generation time: {generationMs:0.00} ms");

        // Antal noder med kun et valg
        sb.AppendLine($"Nodes with only 1 choice: {oneChoiceNodes}");

        // Encounter-tællinger (hvis tilgængelige)
        if (encounterCounts != null && encounterCounts.Count > 0)
        {
            sb.AppendLine("Encounters:");
            foreach (var kvp in encounterCounts)
            {
                sb.AppendLine($"{kvp.Key}: {kvp.Value}");
            }
        }
        else
        {
            sb.AppendLine("Encounters: (none / not assigned)");
        }

        // Sidste klik-interval
        if (lastIntervalSeconds > 0f)
            sb.AppendLine($"Last click: {lastIntervalSeconds:0.0} s");
        else
            sb.AppendLine("Last click: -");

        // Kørsel-statistikker — kun meningsfulde hvis run er fuldført
        if (runCompleted)
        {
            sb.AppendLine($"Run total: {totalRunSeconds:0.0} s");
            sb.AppendLine($"Avg between clicks: {avgIntervalSeconds:0.0} s");
            sb.AppendLine($"Median between clicks: {medianIntervalSeconds:0.0} s");
        }
        else
        {
            sb.AppendLine("Run total: -");
            sb.AppendLine("Avg between clicks: -");
            sb.AppendLine("Median between clicks: -");
        }

        // Vis valideringsresultat afhængigt af valgte generation mode
        switch (selectedMode)
        {
            case MapManager.EncounterGenerationMode.OptionA:
                sb.AppendLine(optionAPass ? "Option A: PASS" : "Option A: FAIL");
                break;

            case MapManager.EncounterGenerationMode.OptionB:
                sb.AppendLine(optionBPass ? "Option B: PASS" : "Option B: FAIL");
                break;

            case MapManager.EncounterGenerationMode.OptionC:
                sb.AppendLine(optionCPass ? "Option C: PASS" : "Option C: FAIL");
                break;

            case MapManager.EncounterGenerationMode.OptionD:
                sb.AppendLine(optionDPass ? "Option D: PASS" : "Option D: FAIL");
                break;

            default:
                sb.AppendLine("Option: -");
                break;
        }

        // Sæt tekst i TMP-komponenten (opdater UI)
        infoText.text = sb.ToString();
    }
}
