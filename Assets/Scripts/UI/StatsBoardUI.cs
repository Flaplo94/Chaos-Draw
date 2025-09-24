using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class StatsBoardUI : MonoBehaviour
{
    [Header("Stats Texts (values only)")]
    [SerializeField] private TMP_Text timeValue;
    [SerializeField] private TMP_Text shardsValue;
    [SerializeField] private TMP_Text waveValue;
    [SerializeField] private TMP_Text bossesValue;

    [Header("Buttons")]
    [SerializeField] private Button retryButton;
    [SerializeField] private Button exitButton;

    private void Awake()
    {
        retryButton.onClick.AddListener(RetryGame);
        exitButton.onClick.AddListener(ExitToMenu);

        gameObject.SetActive(false);
    }

    public void Show(int chaosShards, int wave, int bosses, float time)
    {
        // Formatér tid som mm:ss
        System.TimeSpan t = System.TimeSpan.FromSeconds(time);
        string formattedTime = string.Format("{0:D2}:{1:D2}", t.Minutes, t.Seconds);

        // Sæt KUN values (labels står i UI)
        timeValue.text = formattedTime;
        shardsValue.text = chaosShards.ToString();
        waveValue.text = wave.ToString();
        bossesValue.text = bosses.ToString();

        gameObject.SetActive(true);
    }

    private void RetryGame()
    {
        Time.timeScale = 1f;
        WaveManager.Instance = null;
        ShopManager.Instance = null;
        DeckManager.Instance = null;
        GameOverManager.Instance = null;

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex, LoadSceneMode.Single);
    }

    private void ExitToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}
