using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject gameOverPanel;   // Panelet der vises først (med Score, Retry, Exit)
    [SerializeField] private GameObject statsBoardPanel; // Panelet med stats + Retry/Exit

    [Header("Buttons - GameOver")]
    [SerializeField] private Button scoreButton;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button exitButton;

    [Header("Buttons - StatsBoard")]
    [SerializeField] private Button retryStatsButton;
    [SerializeField] private Button exitStatsButton;

    private void Awake()
    {
        // Sørg for at panelerne starter korrekt
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (statsBoardPanel != null) statsBoardPanel.SetActive(false);

        // Knap events
        if (scoreButton != null) scoreButton.onClick.AddListener(OpenStatsBoard);
        if (retryButton != null) retryButton.onClick.AddListener(RetryGame);
        if (exitButton != null) exitButton.onClick.AddListener(ExitToMenu);

        if (retryStatsButton != null) retryStatsButton.onClick.AddListener(RetryGame);
        if (exitStatsButton != null) exitStatsButton.onClick.AddListener(ExitToMenu);
    }

    // Vises når spilleren dør (kaldes fra GameOverManager.TriggerGameOver)
    public void ShowGameOver()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (statsBoardPanel != null) statsBoardPanel.SetActive(false);
    }

    private void OpenStatsBoard()
    {
        gameOverPanel.SetActive(false);
        statsBoardPanel.SetActive(true);

        // Hent data fra GameOverManager og vis stats
        if (GameOverManager.Instance != null)
            GameOverManager.Instance.ShowStatsBoard();
    }

    private void RetryGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void ExitToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}
