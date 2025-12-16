using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro; // remove if you're using legacy Text instead

public class MapBottomInfo : MonoBehaviour
{
    [SerializeField] private TMP_Text infoText; // drag your text component here

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
        bool optionCPass,
        bool optionBPass,
        MapManager.EncounterGenerationMode selectedMode)
    {
        if (infoText == null)
        {
            Debug.LogWarning("MapBottomInfo has no infoText assigned.");
            return;
        }

        var sb = new StringBuilder();

        // Generation time
        sb.AppendLine($"Generation time: {generationMs:0.00} ms");

        // Nodes with only 1 choice
        sb.AppendLine($"Nodes with only 1 choice: {oneChoiceNodes}");

        // Encounters
        if (encounterCounts != null && encounterCounts.Count > 0)
        {
            sb.AppendLine("Encounters: ");
            bool first = true;
            foreach (var kvp in encounterCounts)
            {
                if (!first) sb.Append("");
                first = false;
                sb.AppendLine($"{kvp.Key}: {kvp.Value}");
            }
        }
        else
        {
            sb.AppendLine("Encounters: (none / not assigned)");
        }

        // Last click interval (always based on last valid move if any)
        if (lastIntervalSeconds > 0f)
            sb.AppendLine($"Last click: {lastIntervalSeconds:0.0} s");
        else
            sb.AppendLine("Last click: -");

        // Timing stats (only if run completed)
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

            default:
                // If you add OptionD later and want something here, you can.
                sb.AppendLine("Option: -");
                break;
        }
        infoText.text = sb.ToString();
    }


}
