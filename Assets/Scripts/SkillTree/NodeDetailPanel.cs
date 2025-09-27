using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChaosDraw.SkillTree;

public class NodeDetailPanel : MonoBehaviour
{
    [Header("UI Refs")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descText;      // description + evtl. level-visning
    [SerializeField] private TMP_Text costText;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text levelBadgeText; // "x/Max" (kun for flade stats og card_reroll)
    [SerializeField] private Button unlockButton;

    private NodeData current;

    private const string RerollNodeId = "card_reroll";

    void Awake()
    {
        if (unlockButton != null)
        {
            unlockButton.onClick.RemoveListener(OnUnlockClicked);
            unlockButton.onClick.AddListener(OnUnlockClicked);
        }
    }

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
        var mgr = SkillTreeManager.Instance;
        if (mgr == null) return;

        if (mgr.UnlockOrUpgrade(current))
            Refresh();
    }

    private void Refresh()
    {
        var mgr = SkillTreeManager.Instance;
        if (current == null || mgr == null)
        {
            Hide();
            return;
        }

        bool isReroll = current.id == RerollNodeId;
        bool isFlat = mgr.IsFlatStat(current);

        int curLv = mgr.GetLevel(current.id);
        int maxLv = Mathf.Max(1, current.maxLevel);

        // Title & icon
        if (titleText) titleText.text = string.IsNullOrEmpty(current.displayName) ? current.id : current.displayName;
        if (iconImage) iconImage.sprite = current.icon;

        // Badge: vis for flade stats og reroll (så spilleren ser den kan opgraderes)
        if (levelBadgeText)
        {
            bool showBadge = isFlat || isReroll;
            levelBadgeText.gameObject.SetActive(showBadge);
            if (showBadge) levelBadgeText.text = $"{curLv}/{maxLv}";
        }

        // Description + levels
        if (descText)
        {
            string baseDesc = string.IsNullOrEmpty(current.description) ? "" : current.description.Trim();
            string levelBlock = isReroll
                ? BuildRerollLevelBlock(current, curLv)
                : BuildLevelBlockGeneric(current, isFlat, curLv);

            if (!string.IsNullOrEmpty(baseDesc) && !string.IsNullOrEmpty(levelBlock))
                descText.text = baseDesc + "\n\n" + levelBlock;
            else
                descText.text = string.IsNullOrEmpty(baseDesc) ? levelBlock : baseDesc;
        }

        // Cost / status
        if (costText)
        {
            if ((isFlat || isReroll) && curLv >= maxLv)
            {
                costText.text = "<color=#7CFC00>Max level reached</color>";
            }
            else
            {
                if (!isFlat && !isReroll && curLv >= maxLv)
                {
                    costText.text = "";
                }
                else
                {
                    int cost = mgr.GetNextCost(current);
                    string unit = isFlat ? "XP" : "Shards"; // Reroll følger non-flat (Shards)
                    costText.text = $"Cost: {cost} {unit}";
                }
            }
        }

        if (unlockButton)
            unlockButton.interactable = CanUpgrade(current, isFlat, isReroll);
    }

    private bool CanUpgrade(NodeData node, bool isFlat, bool isReroll)
    {
        var mgr = SkillTreeManager.Instance;
        if (mgr == null || node == null) return false;

        int cur = mgr.GetLevel(node.id);
        int max = Mathf.Max(1, node.maxLevel);

        if (cur >= max) return false;
        if (cur > 0) return true;
        return mgr.ArePrerequisitesMet(node);
    }

    private string BuildLevelBlockGeneric(NodeData node, bool isFlat, int curLv)
    {
        if (node.valuePerLevel == null || node.valuePerLevel.Count == 0)
            return "";

        var sb = new System.Text.StringBuilder();

        if (isFlat)
        {
            for (int i = 0; i < node.valuePerLevel.Count; i++)
            {
                float raw = node.valuePerLevel[i];
                string vTxt = FormatFlatValue(node.effectKey, raw);

                bool owned = (i < curLv);
                if (owned) sb.Append("<b>");
                sb.Append("Lv" + (i + 1) + ":" + vTxt);
                if (owned) sb.Append("</b>");

                if (i < node.valuePerLevel.Count - 1) sb.Append("   ");
            }
        }
        else
        {
            for (int i = 0; i < node.valuePerLevel.Count; i++)
            {
                float raw = node.valuePerLevel[i];
                string vTxt = FormatFlatValue(node.effectKey, raw);
                sb.AppendLine("• Lv " + (i + 1) + ": " + vTxt);
            }
        }

        return sb.ToString().TrimEnd();
    }

    // Specifik visning for Card Reroll (uafhængig af valuePerLevel-data)
    private string BuildRerollLevelBlock(NodeData node, int curLv)
    {
        int maxLv = Mathf.Max(1, node.maxLevel);
        var sb = new System.Text.StringBuilder();
        for (int i = 1; i <= maxLv; i++)
        {
            string line = "Lv " + i + ": " + i + " reroll/run";
            if (i <= curLv) line = "<b>" + line + "</b>";
            sb.AppendLine("• " + line);
        }
        return sb.ToString().TrimEnd();
    }

    // Formatter for flade stats (uændret)
    private static string FormatFlatValue(string effectKey, float raw)
    {
        string k = (effectKey ?? "").ToLowerInvariant().Replace("_", "").Replace(" ", "");

        bool isHpRegen =
            k.Contains("hpregen") || k.Contains("healthregen") ||
            k == "flatregen" || k == "regen";

        if (isHpRegen)
        {
            return "+" + raw.ToString("0.#");
        }

        float v = raw;
        if (v >= 1f && v <= 100f) v = v / 100f;
        return "+" + (v * 100f).ToString("0.#") + "%";
    }
}
