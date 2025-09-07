using UnityEngine;

public class GameOverManager : MonoBehaviour
{
    public static GameOverManager Instance;

    [Header("UI References")]
    [SerializeField] private GameObject dimmer;       // sort overlay
    [SerializeField] private StatsBoardUI statsBoard; // viser stats
    [SerializeField] private GameOverUI gameOverUI;   // håndterer panel-skift og knapper

    [Header("HUD Elements")]
    [SerializeField] private GameObject waveCounterUI;
    [SerializeField] private GameObject healthBarUI;
    [SerializeField] private GameObject bossHealthBarUI;

    private float runStartTime;

    // gemte stats
    private int cachedWaves;
    private int cachedShards;
    private int cachedBosses;
    private float cachedTime;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (dimmer != null) dimmer.SetActive(false);

        // StatsBoard skal altid starte skjult
        if (statsBoard != null)
            statsBoard.gameObject.SetActive(false);

        runStartTime = Time.time;
    }

    /// <summary>
    /// Kaldes når spillet slutter (fra PlayerHealth eller WaveManager)
    /// </summary>
    public void TriggerGameOver(int wavesCleared, int shardsEarned, int bossesKilled)
    {
        // Sluk HUD
        waveCounterUI?.SetActive(false);
        healthBarUI?.SetActive(false);
        bossHealthBarUI?.SetActive(false);

        // Pause spillet
        Time.timeScale = 0f;

        // Tænd dimmer
        if (dimmer != null) dimmer.SetActive(true);

        // Beregn tid
        cachedTime = Time.time - runStartTime;
        cachedWaves = wavesCleared;
        cachedShards = shardsEarned;
        cachedBosses = bossesKilled;

        // Vis først GameOver-panelet
        if (gameOverUI != null)
            gameOverUI.ShowGameOver();
    }

    /// <summary>
    /// Kaldes af GameOverUI når spilleren trykker "Score"
    /// </summary>
    public void ShowStatsBoard()
    {
        if (statsBoard != null)
            statsBoard.Show(cachedShards, cachedWaves, cachedBosses, cachedTime);
    }
}
