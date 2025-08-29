using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class SkillDetailPanel : MonoBehaviour
{
    public static SkillDetailPanel Instance;

    [Header("UI Refs")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private Image icon;
    [SerializeField] private Button purchaseButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private TMP_Text feedbackText; // Feedback tekst
    [SerializeField] private TMP_Text shardsText;
    [SerializeField] private Image shardIcon;

    private SkillData currentSkill;

    void Awake()
    {
        Instance = this;
        gameObject.SetActive(false);
        if (feedbackText != null) feedbackText.gameObject.SetActive(false);
    }

    public void Open(SkillData skill)
    {
        currentSkill = skill;
        if (skill == null) return;

        gameObject.SetActive(true);

        titleText.text = skill.displayName;
        descText.text = skill.description;
        if (icon != null) icon.sprite = skill.icon;

        // Nulstil feedback når nyt skill åbnes
        if (feedbackText != null)
            feedbackText.gameObject.SetActive(false);

        RefreshShardsUI();

        // === Håndter state for buy-knap ===
        bool isUnlocked = SkillTreeManager.Instance.IsUnlocked(skill.internalID);
        bool prereqsMet = SkillTreeManager.Instance.ArePrerequisitesMet(skill);

        if (isUnlocked)
        {
            purchaseButton.interactable = false;
            costText.text = "<color=green>Unlocked</color>";
        }
        else if (!prereqsMet)
        {
            purchaseButton.interactable = false;
            costText.text = $"Cost: {skill.cost} <color=red>(Locked)</color>";
        }
        else
        {
            purchaseButton.interactable = true;
            costText.text = $"Cost: {skill.cost}";
        }
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    public void OnPurchasePressed()
    {
        if (currentSkill == null) return;

        // Tjek shards
        if (!MetaProgressionManager.Instance.SpendShards(currentSkill.cost))
        {
            ShowFeedback("Not enough Chaos Shards!", Color.red);
            return;
        }

        // Unlock skill
        SkillTreeManager.Instance.Unlock(currentSkill);
        ShowFeedback("Skill unlocked!", Color.green);

        // Efter unlock: disable køb og opdater UI
        purchaseButton.interactable = false;
        costText.text = "<color=green>Unlocked</color>";
    }

    public void RefreshShardsUI()
    {
        if (MetaProgressionManager.Instance != null && shardsText != null)
            shardsText.text = MetaProgressionManager.Instance.GetShards().ToString();
    }

    private void ShowFeedback(string msg, Color color)
    {
        if (feedbackText == null) return;

        StopAllCoroutines();
        feedbackText.color = color;
        StartCoroutine(ShowFeedbackRoutine(msg));
    }

    private IEnumerator ShowFeedbackRoutine(string msg)
    {
        feedbackText.text = msg;
        feedbackText.gameObject.SetActive(true);
        Debug.Log("[SkillDetailPanel] Viser feedback: " + msg);

        yield return new WaitForSeconds(2f);

        feedbackText.gameObject.SetActive(false);
    }
}
