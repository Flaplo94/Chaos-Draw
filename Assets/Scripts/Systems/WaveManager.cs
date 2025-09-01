using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance;

    [SerializeField] private CardHandUI cardHandUI;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private float minSpawnDistance = 3f;
    [SerializeField] private float spawnRadius = 10f;

    [Header("Enemies")]
    public GameObject[] enemyPrefabs;

    [Header("Spawn weights must match enemyPrefabs order!")]
    [SerializeField] private float[] enemySpawnWeights = new float[] { 1f, 1f, 0.3f, 0.1f };

    [Header("Boss Prefabs")]
    [SerializeField] private GameObject bossPrefab;        // First boss (wave 10, 30, 40, …)
    [SerializeField] private GameObject secondBossPrefab;  // Special boss at wave 20
    public Transform bossSpawnPoint;
    public GameObject bossHealthBarUI;

    private bool bossSpawned = false;
    private GameObject currentBoss;

    [Header("Music")]
    [SerializeField] private GameMusicManager musicManager;
    [SerializeField] private AudioClip normalMusic;
    [SerializeField] private AudioClip boss1Music;
    [SerializeField] private AudioClip boss2Music;

    [Header("Waves")]
    public int startEnemyCount = 3;
    public float timeBetweenWaves = 2f;

    private int singleTypeIndex = -1;
    private int unlockedEnemyTypes = 2;

    private int currentWave = 0;
    private readonly List<GameObject> enemiesInWave = new List<GameObject>();
    private bool waveInProgress = false;

    [Header("UI")]
    public TextMeshProUGUI waveText;

    public event Action<int> OnWaveStarted;
    public event Action<int> OnWaveCompleted;
    public event Action<int> OnWaveChanged;

    [Header("Scene Transition")]
    [SerializeField] public string afterWave20Scene = "TYscene"; // set in Inspector

    public int CurrentWave => currentWave;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
        Debug.Log("[Wave] Awake");
    }

    void Start()
    {
        // start normal gameplay music
        if (musicManager != null && normalMusic != null)
            musicManager.PlayMusic(normalMusic);

        StartCoroutine(NextWave());
    }

    void Update()
    {
        // === TEST CHEATS ===
        if (Input.GetKeyDown(KeyCode.L)) // L = jump to wave 10 boss
        {
            Debug.Log("[WaveManager] CHEAT: Skipping to wave 10 boss");
            currentWave = 9; // next wave will be wave 10
            StartCoroutine(NextWave());
        }

        if (Input.GetKeyDown(KeyCode.K)) // K = jump to wave 20 boss
        {
            Debug.Log("[WaveManager] CHEAT: Skipping to wave 20 boss");
            currentWave = 19; // next wave will be wave 20
            StartCoroutine(NextWave());
        }

        if (Input.GetKeyDown(KeyCode.U)) // U = unlock Legacy Deck uden kamp
        {
            Debug.Log("[WaveManager] CHEAT: Force unlock Legacy Deck");
            MetaProgressionManager.Instance.UnlockLegacy();
        }
        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log("FORCE LOADING TEST SCENE");
            SceneManager.LoadScene(afterWave20Scene);
        }

        // Boss wave handling
        if (bossSpawned)
        {
            if (currentBoss == null)
            {
                if (bossHealthBarUI.activeSelf)
                    bossHealthBarUI.SetActive(false);

                if (bossSpawned) // boss just died
                {
                    bossSpawned = false;

                    // Return to normal music
                    if (musicManager != null && normalMusic != null)
                        musicManager.PlayMusic(normalMusic);

                    if (cardHandUI != null) cardHandUI.OnWaveCompleted();

                    OnWaveCompleted?.Invoke(currentWave);
                    OnWaveChanged?.Invoke(currentWave);

                    TryOpenShopOrStartNextWave();
                }
                return;
            }
            if (currentBoss == null && enemiesInWave.Count == 0)
            {
                bossSpawned = false;

                if (cardHandUI != null) cardHandUI.OnWaveCompleted();

                OnWaveCompleted?.Invoke(currentWave);
                OnWaveChanged?.Invoke(currentWave);

                if (currentWave == 20)
                {
                    SceneManager.LoadScene("TYscene");
                }
                else
                {
                    TryOpenShopOrStartNextWave();
                }
            }

            return;
        }

        // Normal wave end
        if (waveInProgress && enemiesInWave.Count == 0)
        {
            waveInProgress = false;

            CardHandUI cardUI = FindFirstObjectByType<CardHandUI>();
            if (cardUI != null)
                cardUI.OnWaveCompleted();

            OnWaveCompleted?.Invoke(currentWave);
            OnWaveChanged?.Invoke(currentWave);

            TryOpenShopOrStartNextWave();
        }
    }

    IEnumerator NextWave()
    {
        yield return new WaitForSeconds(timeBetweenWaves);

        currentWave++;
        if (waveText != null) waveText.text = currentWave.ToString();

        OnWaveStarted?.Invoke(currentWave);
        OnWaveChanged?.Invoke(currentWave);

        if (currentWave % 5 == 0 && unlockedEnemyTypes < enemyPrefabs.Length)
            unlockedEnemyTypes++;

        int maxIndex = Mathf.Min(unlockedEnemyTypes, enemyPrefabs.Length);

        // Boss waves
        if (currentWave % 10 == 0)
        {
            bossHealthBarUI.SetActive(true);

            if (currentWave == 20)
            {
                // Special boss at wave 20
                currentBoss = Instantiate(secondBossPrefab, bossSpawnPoint.position, Quaternion.identity);

                if (musicManager != null && boss2Music != null)
                    musicManager.PlayMusic(boss2Music);
            }
            else
            {
                // Boss at wave 10, 30, 40, …
                currentBoss = Instantiate(bossPrefab, bossSpawnPoint.position, Quaternion.identity);

                if (musicManager != null && boss1Music != null)
                    musicManager.PlayMusic(boss1Music);
            }

            BossSpawner spawner = currentBoss.GetComponent<BossSpawner>();
            if (spawner != null) spawner.waveManager = this;

            bossSpawned = true;

            Slider bossSlider = bossHealthBarUI.GetComponentInChildren<Slider>();
            TextMeshProUGUI bossNameText = bossHealthBarUI.GetComponentInChildren<TextMeshProUGUI>();

            BossHealth bossHealth = currentBoss.GetComponent<BossHealth>();
            bossHealth.AssignHealthBar(bossSlider, bossNameText);

            Camera.main.GetComponent<CameraFollow>().FocusTemporarily(bossSpawnPoint.position, 2.5f);
            singleTypeIndex = -1;
            yield break;
        }

        // Normal wave
        int enemyCount = startEnemyCount + currentWave * 2;

        bool singleTypeWave = (currentWave % 5 == 0);
        if (singleTypeWave)
            singleTypeIndex = GetWeightedRandomIndex(maxIndex);
        else
            singleTypeIndex = -1;

        enemiesInWave.Clear();

        for (int i = 0; i < enemyCount; i++)
        {
            Vector2 spawnOffset;
            do { spawnOffset = UnityEngine.Random.insideUnitCircle * spawnRadius; }
            while (spawnOffset.magnitude < minSpawnDistance);

            Vector2 spawnPos = (Vector2)playerTransform.position + spawnOffset;

            GameObject chosenPrefab = (singleTypeIndex >= 0)
                ? enemyPrefabs[singleTypeIndex]
                : enemyPrefabs[GetWeightedRandomIndex(maxIndex)];

            GameObject enemy = Instantiate(chosenPrefab, spawnPos, Quaternion.identity);
            enemiesInWave.Add(enemy);

            var eh = enemy.GetComponent<EnemyHealth>();
            if (eh != null) eh.OnDeath += () => enemiesInWave.Remove(enemy);
        }

        waveInProgress = true;
    }

    private int GetWeightedRandomIndex(int maxIndex)
    {
        float totalWeight = 0f;
        for (int i = 0; i < maxIndex; i++) totalWeight += enemySpawnWeights[i];

        float randomValue = UnityEngine.Random.value * totalWeight;
        float cumulative = 0f;
        for (int i = 0; i < maxIndex; i++)
        {
            cumulative += enemySpawnWeights[i];
            if (randomValue < cumulative) return i;
        }
        return maxIndex - 1;
    }

    private void TryOpenShopOrStartNextWave()
    {
        if (currentWave > 0 && currentWave % 5 == 0 && ShopManager.Instance != null)
        {
            ShopManager.Instance.OnClosed -= HandleShopClosedAfterWave;
            ShopManager.Instance.OnClosed += HandleShopClosedAfterWave;
            ShopManager.Instance.Open();
        }
        else
        {
            StartCoroutine(NextWave());
        }
    }

    private void HandleShopClosedAfterWave()
    {
        if (ShopManager.Instance != null)
            ShopManager.Instance.OnClosed -= HandleShopClosedAfterWave;

        StartCoroutine(NextWave());
    }

    public void RegisterMinion(GameObject minion)
    {
        if (minion == null) return;
        enemiesInWave.Add(minion);

        var eh = minion.GetComponent<EnemyHealth>();
        if (eh != null) eh.OnDeath += () => enemiesInWave.Remove(minion);
    }

    public void EndRun()
    {
        int wavesCleared = currentWave;
        int reward = 0;

        reward += wavesCleared;
        reward += (wavesCleared / 10) * 5;

        if (reward > 0)
            MetaProgressionManager.Instance.AddShards(reward);

        Debug.Log($"[WaveManager] Run ended after wave {wavesCleared}. Reward: {reward} Chaos Shards.");

        if ((currentWave + 1) >= 20 && !string.IsNullOrEmpty(afterWave20Scene))
        {
            SceneManager.LoadScene(afterWave20Scene);
            return;
        }

        if (GameOverManager.Instance != null)
            GameOverManager.Instance.TriggerGameOver(wavesCleared, reward);
    }
    public void OnBossDied()
    {
        currentBoss = null;
        bossSpawned = false;

        if (currentWave == 20 && !string.IsNullOrEmpty(afterWave20Scene))
        {
            SceneManager.LoadScene(afterWave20Scene);
        }
        else
        {
            if (musicManager != null && normalMusic != null)
                musicManager.PlayMusic(normalMusic);

            TryOpenShopOrStartNextWave();
        }
    }
}
