using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro; // remove if you're using legacy Text instead

public class MapBottomInfo : MonoBehaviour
{
    [SerializeField] private TMP_Text infoText; // drag your text component here

    public void ShowInfo(long generationMs, Dictionary<string, int> encounterCounts)
    {
        if (infoText == null)
        {
            Debug.LogWarning("MapBottomInfo has no infoText assigned.");
            return;
        }

        var sb = new StringBuilder();

        // Line 1: generation time
        sb.AppendLine($"Generation time: {generationMs} ms");

        // Line 2+: encounter breakdown
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
