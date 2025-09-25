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

    [Header("Debug")]
    [SerializeField] private bool verboseLogs = false;

    // --- Unity lifecycle ---
    private void Awake()
    {
        // Sikre at vi ikke får dobbelt-registreringer, hvis man binder i Inspector
        if (skillTreeButton != null)
        {
            skillTreeButton.onClick.RemoveListener(OpenSkillTree);
            skillTreeButton.onClick.AddListener(OpenSkillTree);
        }
    }

    private void Start()
    {
        // Start-tilstand: Main menu synlig, skill tree skjult
        SetGroup(mainMenuGroup, true);
        SetGroup(skillTreeGroup, false);

        RefreshLocks();     // læs gemt meta og opdatér ikon/knap
        if (verboseLogs) LogState("[Start]");
    }

    private void OnEnable()
    {
        // Når hovedmenuen vises igen efter et run: opdatér låse UI
        RefreshLocks();
        if (verboseLogs) LogState("[OnEnable]");
    }

    // --- Public UI handlers ---
    public void OpenSkillTree()
    {
        bool unlocked = MetaProgressionManager.Instance != null && MetaProgressionManager.Instance.skillTreeUnlocked;
        if (!unlocked)
        {
            if (verboseLogs) Debug.Log("[SkillTreeToggle] Skill Tree er låst – ignorer klik.");
            return;
        }

        if (verboseLogs) Debug.Log("[SkillTreeToggle] Åbner Skill Tree");
        SetGroup(mainMenuGroup, false);
        SetGroup(skillTreeGroup, true);

        // Ekstra sikkerhed mod andre scripts der evt. toggler i samme frame
        StartCoroutine(ForceShowNextFrame());
    }

    public void CloseSkillTreeAndShowMenu()
    {
        if (verboseLogs) Debug.Log("[SkillTreeToggle] Lukker Skill Tree  tilbage til menu");
        SetGroup(skillTreeGroup, false);
        SetGroup(mainMenuGroup, true);
    }

    // --- Core: sync låse UI med gemt meta ---
    public void RefreshLocks()
    {
        bool hasMeta = MetaProgressionManager.Instance != null;

        bool skillTreeUnlocked = hasMeta && MetaProgressionManager.Instance.skillTreeUnlocked;
        if (skillTreeButton != null) skillTreeButton.interactable = skillTreeUnlocked;
        if (skillTreeLockIcon != null) skillTreeLockIcon.SetActive(!skillTreeUnlocked);

        // Valgfrit: Deck of Fate samme mønster (ændrer ikke andre systemer)
        bool deckUnlocked = hasMeta && MetaProgressionManager.Instance.DeckOfFateUnlocked;
        if (DeckOfFateButton != null) DeckOfFateButton.interactable = deckUnlocked;
        if (DeckOfFateLockIcon != null) DeckOfFateLockIcon.SetActive(!deckUnlocked);

        if (verboseLogs)
        {
            Debug.Log($"[SkillTreeToggle] RefreshLocks()  skillTreeUnlocked={skillTreeUnlocked}, deckUnlocked={deckUnlocked}");
        }
    }

    // --- Helpers ---
    private void SetGroup(GameObject go, bool show)
    {
        if (!go) return;
        go.SetActive(show);
    }

    private IEnumerator ForceShowNextFrame()
    {
        // I tilfælde af at andre scripts sætter canvases i OnEnable/Start samme frame
        yield return null; // 1 frame
        SetGroup(mainMenuGroup, false);
        SetGroup(skillTreeGroup, true);
        if (verboseLogs) LogState("[ForceShowNextFrame]");
    }

    private void LogState(string where)
    {
        string PathOf(GameObject go)
        {
            if (!go) return "(null)";
            var t = go.transform;
            string p = t.name;
            while (t.parent)
            {
                t = t.parent;
                p = t.name + "/" + p;
            }
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
