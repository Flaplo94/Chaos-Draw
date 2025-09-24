using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenu : MonoBehaviour
{
    [Header("Scene Names")]
    public string gameScene = "GameScene"; // din testscene
    public string DeckOfFateScene = "DeckOfFate";
    public string skillTreeScene = "SkillTree";
    public string optionsScene = "Options";

    [Header("Unlockable Buttons")]
    public Button DeckOfFateButton;      // drag DeckOfFate Button her
    public Button skillTreeButton;       // drag SkillTreeButton her

    [Header("Lock Icons")]
    public GameObject DeckOfFateLockIcon;    // drag DeckOfFateButton/LockIcon her
    public GameObject skillTreeLockIcon; // drag SkillTreeButton/LockIcon her

    [Header("Texts")]
    public TMP_Text DeckOfFateText;      // drag TMP_Text fra DeckOfFateButton
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

        // --- Deck Of Fate unlock (via Fire Spiral node) ---
        bool DeckOfFateUnlocked = MetaProgressionManager.Instance.DeckOfFateUnlocked;
        if (DeckOfFateButton) DeckOfFateButton.interactable = DeckOfFateUnlocked;
        if (DeckOfFateText) DeckOfFateText.color = DeckOfFateUnlocked ? unlockedColor : lockedColor;
        if (DeckOfFateLockIcon) DeckOfFateLockIcon.SetActive(!DeckOfFateUnlocked);

        
    }

    // --- Button events ---
    public void PlayGame() => SceneManager.LoadScene("GameScene");

    public void OpenDeckOfFate()
    {
        if (MetaProgressionManager.Instance != null && MetaProgressionManager.Instance.DeckOfFateUnlocked)
            SceneManager.LoadScene(DeckOfFateScene);
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
