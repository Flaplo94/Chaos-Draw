using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro; // remove if you're using legacy Text instead

public class MapBottomInfo : MonoBehaviour
{
    [SerializeField] private TMP_Text infoText; // drag your text component here

    public void ShowInfo(long generationMs, int oneChoiceNodes, Dictionary<string, int> encounterCounts)
    {
        if (infoText == null)
        {
            Debug.LogWarning("MapBottomInfo has no infoText assigned.");
            return;
        }

        var sb = new StringBuilder();

        // generation time
        sb.AppendLine($"Generation time: {generationMs} ms");

        // Nodes with only 1 possible next step
        sb.AppendLine($"Nodes with only 1 choice: {oneChoiceNodes}");

        // encounter breakdown
        if (encounterCounts != null && encounterCounts.Count > 0)
        {
            sb.Append("Encounters: ");

            bool first = true;
            foreach (var kvp in encounterCounts)
            {
                if (!first) sb.Append(" | ");
                first = false;
                sb.Append($"{kvp.Key}: {kvp.Value}");
            }
        }
        else
        {
            sb.Append("Encounters: (none / not assigned)");
        }

        

        infoText.text = sb.ToString();
    }
}
