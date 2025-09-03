using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameOverManager : MonoBehaviour
{
    public static GameOverManager Instance;

    [Header("UI References")]
    [SerializeField] private GameObject gameOverUI;
    [SerializeField] private GameObject dimmer;
    [SerializeField] private TextMeshProUGUI unlockMessage; // felt til unlock-besked

    [Header("Value Fields (drag TMP her)")]
    [SerializeField] private TextMeshProUGUI waveValue;
    [SerializeField] private TextMeshProUGUI shardsValue;
    [SerializeField] private TextMeshProUGUI timeValue;

    [Header("HUD Elements")]
    [SerializeField] private GameObject waveCounterUI;
    [SerializeField] private GameObject healthBarUI;
    [SerializeField] private GameObject bossHealthBarUI;
    private float runStartTime;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (gameOverUI != null) gameOverUI.SetActive(false);
        if (dimmer != null) dimmer.SetActive(false);

        runStartTime = Time.time;
    }

    public void TriggerGameOver(int wavesCleared, int shardsEarned)
    {
        
        // Sluk HUD
        waveCounterUI?.SetActive(false);
        healthBarUI?.SetActive(false);
        bossHealthBarUI?.SetActive(false);

        // Pause spillet
        Time.timeScale = 0f;

        // Tænd dimmer og panel
        if (dimmer != null) dimmer.SetActive(true);
        if (gameOverUI != null) gameOverUI.SetActive(true);

        // Beregn tid
        float secondsSurvived = Time.time - runStartTime;
        System.TimeSpan t = System.TimeSpan.FromSeconds(secondsSurvived);
        string formattedTime = string.Format("{0:D2}:{1:D2}", t.Minutes, t.Seconds);

        // Sæt værdier i felterne
        if (waveValue != null) waveValue.text = wavesCleared.ToString();
        if (shardsValue != null) shardsValue.text = shardsEarned.ToString();
        if (timeValue != null) timeValue.text = formattedTime;

        // === Unlock kun Skill Tree første gang ved wave >= 10 ===
        if (wavesCleared >= 10 && MetaProgressionManager.Instance != null)
        {
            if (!MetaProgressionManager.Instance.skillTreeUnlocked)
            {
                // Første gang  vis besked + gem unlock
                MetaProgressionManager.Instance.skillTreeUnlocked = true;
                MetaProgressionManager.Instance.Save();

                if (unlockMessage != null)
                    unlockMessage.gameObject.SetActive(true);

                
            }
            else
            {
                // Allerede unlocked  skjul besked
                if (unlockMessage != null)
                    unlockMessage.gameObject.SetActive(false);
            }
        }
        else
        {
            if (unlockMessage != null)
                unlockMessage.gameObject.SetActive(false);
        }

        
    }

    // --- Knapper ---
    public void Retry()
    {
        Time.timeScale = 1f;

        // Reset alle run-specifikke managers
        WaveManager.Instance = null;
        ShopManager.Instance = null;
        DeckManager.Instance = null;
        GameOverManager.Instance = null;

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex, LoadSceneMode.Single);
    }

    public void QuitToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu"); // husk at tilføje scenen i Build Settings
    }
}
