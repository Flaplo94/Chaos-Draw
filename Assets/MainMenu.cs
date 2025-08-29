using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenu : MonoBehaviour
{
    [Header("Scene Names")]
    public string gameScene = "Stefan TestScene"; // din testscene
    public string legacyDeckScene = "LegacyDeck";
    public string skillTreeScene = "SkillTree";
    public string optionsScene = "Options";

    [Header("Unlockable Buttons")]
    public Button legacyDeckButton;      // drag LegacyDeckButton her
    public Button skillTreeButton;       // drag SkillTreeButton her

    [Header("Lock Icons")]
    public GameObject legacyLockIcon;    // drag LegacyDeckButton/LockIcon her
    public GameObject skillTreeLockIcon; // drag SkillTreeButton/LockIcon her

    [Header("Texts")]
    public TMP_Text legacyDeckText;      // drag TMP_Text fra LegacyDeckButton
    public TMP_Text skillTreeText;       // drag TMP_Text fra SkillTreeButton

    private Color unlockedColor = Color.white;
    private Color lockedColor = Color.gray;

    void Start()
    {
        RefreshUI();
    }

    void OnEnable()
    {
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (MetaProgressionManager.Instance == null) return;

        // --- Skill Tree unlock (via wave 10) ---
        bool skillTreeUnlocked = MetaProgressionManager.Instance.skillTreeUnlocked;
        if (skillTreeButton) skillTreeButton.interactable = skillTreeUnlocked;
        if (skillTreeText) skillTreeText.color = skillTreeUnlocked ? unlockedColor : lockedColor;
        if (skillTreeLockIcon) skillTreeLockIcon.SetActive(!skillTreeUnlocked);

        // --- Legacy Deck unlock (via Fire Spiral node) ---
        bool legacyUnlocked = MetaProgressionManager.Instance.legacyUnlocked;
        if (legacyDeckButton) legacyDeckButton.interactable = legacyUnlocked;
        if (legacyDeckText) legacyDeckText.color = legacyUnlocked ? unlockedColor : lockedColor;
        if (legacyLockIcon) legacyLockIcon.SetActive(!legacyUnlocked);

        
    }

    // --- Button events ---
    public void PlayGame() => SceneManager.LoadScene(gameScene);

    public void OpenLegacyDeck()
    {
        if (MetaProgressionManager.Instance != null && MetaProgressionManager.Instance.legacyUnlocked)
            SceneManager.LoadScene(legacyDeckScene);
    }

    public void OpenSkillTree()
    {
        if (MetaProgressionManager.Instance != null && MetaProgressionManager.Instance.skillTreeUnlocked)
            SceneManager.LoadScene(skillTreeScene);
    }

    public void OpenOptions() => SceneManager.LoadScene(optionsScene);

    public void ExitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
