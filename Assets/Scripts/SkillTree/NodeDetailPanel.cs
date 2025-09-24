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
    [SerializeField] private TMP_Text levelBadgeText; // "x/Max" (kun for flade stats)
    [SerializeField] private Button unlockButton;

    private NodeData current;

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

        int curLv = mgr.GetLevel(current.id);
        int maxLv = Mathf.Max(1, current.maxLevel);
        bool isFlat = mgr.IsFlatStat(current);

        // Title & icon
        if (titleText) titleText.text = string.IsNullOrEmpty(current.displayName) ? current.id : current.displayName;
        if (iconImage) iconImage.sprite = current.icon;

        // Badge: kun for flade stats
        if (levelBadgeText)
        {
            levelBadgeText.gameObject.SetActive(isFlat);
            if (isFlat) levelBadgeText.text = $"{curLv}/{maxLv}";
        }

        // Description + levels (kompakt linje for flade stats)
        if (descText)
        {
            string baseDesc = string.IsNullOrEmpty(current.description) ? "" : current.description.Trim();
            string levelBlock = BuildLevelBlock(current, isFlat, curLv);

            if (!string.IsNullOrEmpty(baseDesc) && !string.IsNullOrEmpty(levelBlock))
                descText.text = baseDesc + "\n\n" + levelBlock;
            else
                descText.text = string.IsNullOrEmpty(baseDesc) ? levelBlock : baseDesc;
        }

        // Cost / status
        if (costText)
        {
            if (isFlat && curLv >= maxLv)
            {
                // Kun flade stats viser "Max level reached"
                costText.text = "<color=#7CFC00>Max level reached</color>";
            }
            else
            {
                // Låste noder viser pris; allerede-unlocked non-flat (typisk 1/1) skjuler pris
                if (!isFlat && curLv >= maxLv)
                {
                    costText.text = ""; // non-flat og allerede unlocked -> ingen statuslinje
                }
                else
                {
                    int cost = mgr.GetNextCost(current);
                    string unit = isFlat ? "XP" : "Shards";
                    costText.text = $"Cost: {cost} {unit}";
                }
            }
        }

        // Unlock-knap
        if (unlockButton)
            unlockButton.interactable = CanUpgrade(current, isFlat);
    }

    private bool CanUpgrade(NodeData node, bool isFlat)
    {
        var mgr = SkillTreeManager.Instance;
        if (mgr == null || node == null) return false;

        int cur = mgr.GetLevel(node.id);
        int max = Mathf.Max(1, node.maxLevel);

        if (cur >= max) return false;              // færdig
        if (cur > 0) return true;                  // kan altid tage næste level
        return mgr.ArePrerequisitesMet(node);      // første køb kræver prereqs
    }

    /// <summary>
    /// Flade stats: én kompakt linje "Lv1:+X%   Lv2:+Y% ..."
    /// Ikke-flade: kort punktopstilling "• Lv 1: +X%" (hvis de har valuePerLevel),
    /// ellers tom (kun description).
    
    private string BuildLevelBlock(NodeData node, bool isFlat, int curLv)
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
                sb.Append($"Lv{i + 1}:{vTxt}");
                if (owned) sb.Append("</b>");

                if (i < node.valuePerLevel.Count - 1) sb.Append("   ");
            }
        }
        else
        {
            // Bevar en simpel liste kun hvis data findes; ingen "owned", ingen badge
            for (int i = 0; i < node.valuePerLevel.Count; i++)
            {
                float raw = node.valuePerLevel[i];
                string vTxt = FormatFlatValue(node.effectKey, raw);
                sb.AppendLine($"• Lv {i + 1}: {vTxt}");
            }
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Formatter flad-stat værdi:
    /// - flat_hp_regen  "+X"
    /// - øvrige tolkes som procent (0.15 => +15%). Hvis 5..100, tolkes som 5–100%.
    /// </summary>
    private static string FormatFlatValue(string effectKey, float raw)
    {
        // Normalisér nøgle (tåler "flat_hpregEN", "flat_hp_regen", "HPRegen" osv.)
        string k = (effectKey ?? "").ToLowerInvariant().Replace("_", "").Replace(" ", "");

        // Alt der ligner hp-regen/health-regen er FLADT tal (ikke procent)
        bool isHpRegen =
            k.Contains("hpregen") || k.Contains("healthregen") ||
            k == "flatregen" || k == "regen";

        if (isHpRegen)
        {
            // Vis som +X (du kan evt. tilføje "/s" hvis det er pr. sekund)
            return $"+{raw:0.#}";
        }

        // Procenter: understøt både 0.15 => 15% og 5 => 5%
        float v = raw;
        if (v >= 1f && v <= 100f) v = v / 100f; // <-- rettet fra >1 til >=1

        return $"+{(v * 100f):0.#}%";
    }

}
