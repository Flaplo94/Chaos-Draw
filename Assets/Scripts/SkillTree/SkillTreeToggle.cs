using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SkillTreeToggle : MonoBehaviour
{
    [Header("UI Groups (scene objects)")]
    [SerializeField] private GameObject mainMenuGroup;   // MainMenuCanvas/MainMenuGroup
    [SerializeField] private GameObject skillTreeGroup;  // MainMenuCanvas/SkillTreeGroup

    [Header("Buttons & Icons (optional)")]
    [SerializeField] private Button DeckOfFateButton;
    [SerializeField] private GameObject DeckOfFateLockIcon;
    [SerializeField] private Button skillTreeButton;
    [SerializeField] private GameObject skillTreeLockIcon;

    private void Start()
    {
        LogState("[Start] before enforce");
        // Kendt starttilstand
        SetGroup(mainMenuGroup, true);
        SetGroup(skillTreeGroup, false);
        LogState("[Start] after enforce");
    }

    public void OpenSkillTree()
    {
        Debug.Log("[SkillTreeToggle] OpenSkillTree() called");
        SetGroup(mainMenuGroup, false);
        SetGroup(skillTreeGroup, true);           // enable nu
        StartCoroutine(ForceShowNextFrame());     // og igen næste frame (overstyr andre scripts)
    }

    public void CloseSkillTree()
    {
        Debug.Log("[SkillTreeToggle] CloseSkillTree() called");
        SetGroup(skillTreeGroup, false);
        SetGroup(mainMenuGroup, true);
        LogState("[Close] after");
    }

    private IEnumerator ForceShowNextFrame()
    {
        yield return null; // vent én frame, så andres Start() er kørt
        RepairCanvasAndGroup(skillTreeGroup);
        RepairCanvasAndGroup(mainMenuGroup);
        // Sikr endelig tilstand:
        SetGroup(mainMenuGroup, false);
        SetGroup(skillTreeGroup, true);
        LogState("[ForceShowNextFrame] enforced");
    }

    private void SetGroup(GameObject go, bool state)
    {
        if (!go) { Debug.LogWarning("[SkillTreeToggle] SetGroup(null)"); return; }
        if (go.activeSelf != state) go.SetActive(state);
    }

    private void RepairCanvasAndGroup(GameObject group)
    {
        if (!group) return;
        // Hvis nogen har sat en Canvas på gruppen der blev disabled:
        var canvas = group.GetComponentInParent<Canvas>();
        if (canvas && !canvas.enabled) canvas.enabled = true;

        // CanvasGroup der er blevet gennemsigtig?
        var cg = group.GetComponent<CanvasGroup>();
        if (cg)
        {
            cg.alpha = 1f;
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }
    }

    private void LogState(string where)
    {
        string PathOf(GameObject go)
        {
            if (!go) return "null";
            var t = go.transform;
            string p = t.name;
            while (t.parent) { t = t.parent; p = t.name + "/" + p; }
            return p + $" (id:{go.GetInstanceID()})";
        }

        bool menuSelf = mainMenuGroup && mainMenuGroup.activeSelf;
        bool menuHier = mainMenuGroup && mainMenuGroup.activeInHierarchy;
        bool treeSelf = skillTreeGroup && skillTreeGroup.activeSelf;
        bool treeHier = skillTreeGroup && skillTreeGroup.activeInHierarchy;

        Debug.Log($"{where} Menu[{PathOf(mainMenuGroup)}] self={menuSelf} hier={menuHier} | " +
                  $"Tree[{PathOf(skillTreeGroup)}] self={treeSelf} hier={treeHier}");
    }
}
