using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChaosDraw.SkillTree;

public class NodeDetailPanel : MonoBehaviour
{
    [Header("UI Refs")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descText;   // <- vi bruger denne til at vise både description + level-oversigt
    [SerializeField] private TMP_Text costText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Button unlockButton;

    private NodeData current;

    public void Show(NodeData node)
    {
        current = node;
        gameObject.SetActive(true);
        Refresh();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        current = null;
    }

    public void OnUnlockClicked()
    {
        if (current == null) return;
        if (SkillTreeManager.Instance != null)
        {
            if (SkillTreeManager.Instance.UnlockOrUpgrade(current))
                Refresh();
        }
    }

    private void Refresh()
    {
        if (current == null || SkillTreeManager.Instance == null)
        {
            Hide();
            return;
        }

        var mgr = SkillTreeManager.Instance;

        // Title / Icon
        if (titleText) titleText.text = string.IsNullOrEmpty(current.displayName) ? current.id : current.displayName;
        if (iconImage) iconImage.sprite = current.icon;

        // Cost
        if (costText)
        {
            int cost = mgr.GetNextCost(current);
            bool flat = mgr.IsFlatStat(current);
            string unit = flat ? "XP" : "Shards";
            // Hvis maxed eller låst uden prereqs, skjul pris
            bool canUp = CanUpgrade(current);
            costText.text = canUp ? $"Cost: {cost} {unit}" : "";
        }

        // Description + Level-list (for flade stats)
        if (descText)
        {
            var baseDesc = string.IsNullOrEmpty(current.description) ? "" : current.description.Trim();
            string extra = BuildLevelBreakdown(current);
            descText.text = string.IsNullOrEmpty(extra) ? baseDesc : (baseDesc + "\n\n" + extra);
        }

        // Button state
        if (unlockButton) unlockButton.interactable = CanUpgrade(current);
    }

    private bool CanUpgrade(NodeData node)
    {
        var mgr = SkillTreeManager.Instance;
        if (mgr == null || node == null) return false;
        int cur = mgr.GetLevel(node.id);
        int max = Mathf.Max(1, node.maxLevel);
        if (cur >= max) return false;
        // enten er den låst men prereqs opfyldt, eller allerede unlocked og ikke maxed
        return (cur > 0) || mgr.ArePrerequisitesMet(node);
    }

    private string BuildLevelBreakdown(NodeData node)
    {
        var mgr = SkillTreeManager.Instance;
        if (mgr == null || node == null) return "";

        // Kun for flade stats (dem der bruger valuePerLevel)
        if (!mgr.IsFlatStat(node) || node.valuePerLevel == null || node.valuePerLevel.Count == 0)
            return "";

        // Lav linjer: "Level i: +X"
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        int curLv = mgr.GetLevel(node.id);
        int maxLv = Mathf.Max(node.maxLevel, node.valuePerLevel.Count);

        sb.AppendLine("Levels:");
        for (int i = 0; i < maxLv; i++)
        {
            float raw = (i < node.valuePerLevel.Count) ? node.valuePerLevel[i] : 0f;
            string val = FormatFlatValue(node.effectKey, raw);

            // Markér aktuelle/fremtidige trin diskret (valgfrit)
            // ex: " (owned)" på allerede købte levels
            string owned = (i < curLv) ? " (owned)" : "";

            sb.AppendLine($"Level {i + 1}: {val}{owned}");
        }
        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Formatterer en flad-stat-værdi.
    /// - HP-regen: rå HP pr. 5s (heltal)
    /// - Øvrige: procent; 0.15 => +15%. Hvis 5..100 tastes ved en fejl, tolkes det som procent.
    /// </summary>
    private static string FormatFlatValue(string effectKey, float raw)
    {
        if (effectKey == "flat_hp_regen")
        {
            return $"+{raw:0}";
        }

        float v = raw;
        if (v > 1f && v <= 100f) v = v / 100f; // tolerér hvis man kom til at skrive 5 for 5%

        return $"+{(v * 100f):0.#}%";
    }
}
