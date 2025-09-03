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
    public GameObject bossHealthBarUI;

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
        
    }

    void Start()
    {
        if (musicManager != null && normalMusic != null)
            musicManager.PlayMusic(normalMusic);

        StartCoroutine(NextWave());
    }

    void Update()
    {
        // === TEST CHEATS ===
        if (Input.GetKeyDown(KeyCode.L))
        {
            Debug.Log("[WaveManager] CHEAT: Skipping to wave 10 boss");
            currentWave = 9;
            StartCoroutine(NextWave());
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            Debug.Log("[WaveManager] CHEAT: Skipping to wave 20 boss");
            currentWave = 19;
            StartCoroutine(NextWave());
        }

        if (Input.GetKeyDown(KeyCode.U))
        {
            Debug.Log("[WaveManager] CHEAT: Force unlock Legacy Deck");
            MetaProgressionManager.Instance.UnlockLegacy();
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log("FORCE LOADING TEST SCENE");
            SceneManager.LoadScene(afterWave20Scene);
        }

        // ===== Unified wave end check =====
        if (waveInProgress && enemiesInWave.Count == 0)
        {
            waveInProgress = false;

            if (bossHealthBarUI.activeSelf)
                bossHealthBarUI.SetActive(false);

            if (musicManager != null && normalMusic != null)
                musicManager.PlayMusic(normalMusic);

            if (cardHandUI != null) cardHandUI.OnWaveCompleted();

            OnWaveCompleted?.Invoke(currentWave);
            OnWaveChanged?.Invoke(currentWave);

            if (currentWave == 20)
            {
                SceneManager.LoadScene(afterWave20Scene);
            }
            else
            {
                TryOpenShopOrStartNextWave();
            }
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

        enemiesInWave.Clear();

        // Boss waves
        if (currentWave % 10 == 0)
        {
            bossHealthBarUI.SetActive(true);

            // pick a spawn offset further away than normal enemies
            Vector2 bossSpawnOffset;
            float bossMinDistance = minSpawnDistance + 20f;
            float bossRadius = spawnRadius + 20f;

            do
            {
                bossSpawnOffset = UnityEngine.Random.insideUnitCircle * bossRadius;
            }
            while (bossSpawnOffset.magnitude < bossMinDistance);

            Vector3 spawnPosition = playerTransform.position + (Vector3)bossSpawnOffset;

            GameObject bossPrefabToUse = (currentWave == 20) ? secondBossPrefab : bossPrefab;
            GameObject boss = Instantiate(bossPrefabToUse, spawnPosition, Quaternion.identity);

            // 🔥 Register boss just like a minion
            enemiesInWave.Add(boss);

            var bh = boss.GetComponent<BossHealth>();
            if (bh != null) bh.OnDeath += () => enemiesInWave.Remove(boss);

            if (musicManager != null)
            {
                if (currentWave == 20 && boss2Music != null) musicManager.PlayMusic(boss2Music);
                else if (boss1Music != null) musicManager.PlayMusic(boss1Music);
            }

            BossSpawner spawner = boss.GetComponent<BossSpawner>();
            if (spawner != null) spawner.waveManager = this;

            Slider bossSlider = bossHealthBarUI.GetComponentInChildren<Slider>();
            TextMeshProUGUI bossNameText = bossHealthBarUI.GetComponentInChildren<TextMeshProUGUI>();

            BossHealth bossHealth = boss.GetComponent<BossHealth>();
            bossHealth.AssignHealthBar(bossSlider, bossNameText);

            // Camera focus on boss spawn position
            var camFollow = Camera.main.GetComponent<CameraFollow>();
            if (camFollow != null)
            {
                StartCoroutine(PauseDuringFocus(camFollow, spawnPosition, 4f));
            }

            singleTypeIndex = -1;
            waveInProgress = true;
            yield break;
        }

        // Normal wave
        int enemyCount = startEnemyCount + currentWave * 2;

        bool singleTypeWave = (currentWave % 5 == 0);
        if (singleTypeWave)
            singleTypeIndex = GetWeightedRandomIndex(maxIndex);
        else
            singleTypeIndex = -1;

        for (int i = 0; i < enemyCount; i++)
        {
            Vector2 spawnOffset;
            do { spawnOffset = UnityEngine.Random.insideUnitCircle * spawnRadius; }
            while (spawnOffset.magnitude < minSpawnDistance);

            Vector2 spawnPos = (Vector2)playerTransform.position + spawnOffset;

            GameObject chosenPrefab = (singleTypeWave)
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

        

        if ((currentWave + 1) >= 20 && !string.IsNullOrEmpty(afterWave20Scene))
        {
            SceneManager.LoadScene(afterWave20Scene);
            return;
        }

        if (GameOverManager.Instance != null)
            GameOverManager.Instance.TriggerGameOver(wavesCleared, reward);
    }

    private IEnumerator PauseDuringFocus(CameraFollow camFollow, Vector3 target, float duration)
    {
        // Pause game
        Time.timeScale = 0f;

        // Start camera focus
        camFollow.FocusTemporarily(target, duration);

        // Wait in realtime (ignores timescale)
        yield return new WaitForSecondsRealtime(duration);

        // Resume game
        Time.timeScale = 1f;
    }
}
