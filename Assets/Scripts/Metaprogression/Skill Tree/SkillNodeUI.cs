using UnityEngine;
using UnityEngine.UI;

public class SkillNodeUI : MonoBehaviour
{
    [Header("UI Refs")]
    [SerializeField] private Image icon;
    [SerializeField] private GameObject lockedOverlay;
    [SerializeField] private GameObject unlockedCheck;
    [SerializeField] private GameObject availableGlow;
    [SerializeField] private Button button;

    private SkillData skillData;

    void Awake()
    {
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
        }
    }

    public void Init(SkillData data)
    {
        skillData = data;

        if (icon != null && data != null)
            icon.sprite = data.icon;

        Refresh();
    }

    void OnClick()
    {
        if (skillData != null)
            SkillDetailPanel.Instance.Open(skillData);
    }

    public void Refresh()
    {
        if (skillData == null) return;

        bool isUnlocked = SkillTreeManager.Instance.IsUnlocked(skillData.internalID);
        bool prereqsMet = SkillTreeManager.Instance.ArePrerequisitesMet(skillData);

        if (unlockedCheck != null)
            unlockedCheck.SetActive(isUnlocked);

        // overlay kun aktiv hvis skillen er låst OG man mangler prerequisites
        if (lockedOverlay != null)
            lockedOverlay.SetActive(!isUnlocked && !prereqsMet);

        // glow aktiv kun hvis ikke-unlocked og prerequisites er opfyldt
        if (availableGlow != null)
            availableGlow.SetActive(!isUnlocked && prereqsMet);

        // knappen skal altid kunne åbne detailpanelet
        if (button != null)
            button.interactable = true;
    }


    private bool CheckPrerequisites()
    {
        if (skillData.prerequisites == null || skillData.prerequisites.Length == 0)
            return true;

        foreach (var req in skillData.prerequisites)
        {
            if (!SkillTreeManager.Instance.IsUnlocked(req.internalID))
                return false;
        }
        return true;
    }
}
