using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance;

    [SerializeField] private CardHandUI cardHandUI;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private float minSpawnDistance = 3f;
    [SerializeField] private float spawnRadius = 10f;

    public GameObject[] enemyPrefabs;

    [Header("Spawn weights must match enemyPrefabs order!")]
    [SerializeField] private float[] enemySpawnWeights = new float[] { 1f, 1f, 0.3f, 0.1f };
    public GameObject bossPrefab;
    public Transform bossSpawnPoint;
    public GameObject bossHealthBarUI;

    private bool bossSpawned = false;
    private GameObject currentBoss;

    public int startEnemyCount = 3;
    public float timeBetweenWaves = 2f;

    private int singleTypeIndex = -1;
    private int unlockedEnemyTypes = 2;

    private int currentWave = 0;
    private readonly List<GameObject> enemiesInWave = new List<GameObject>();
    private bool waveInProgress = false;

    public TextMeshProUGUI waveText;

    public event Action<int> OnWaveStarted;
    public event Action<int> OnWaveCompleted;
    public event Action<int> OnWaveChanged;

    public int CurrentWave => currentWave;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
        Debug.Log("[Wave] Awake");
    }

    void Start()
    {
        StartCoroutine(NextWave());
    }

    void Update()
    {
        // === TEST CHEATS ===
        if (Input.GetKeyDown(KeyCode.L)) // L = jump to wave 10 boss
        {
            Debug.Log("[WaveManager] CHEAT: Skipping to wave 10 boss");
            currentWave = 9; // næste wave bliver wave 10
            StartCoroutine(NextWave());
        }

        if (Input.GetKeyDown(KeyCode.U)) // U = unlock Legacy Deck uden kamp
        {
            Debug.Log("[WaveManager] CHEAT: Force unlock Legacy Deck");
            MetaProgressionManager.Instance.UnlockLegacy();
        }

        // Boss
        if (bossSpawned)
        {
            if (currentBoss == null)
            {
                bossHealthBarUI.SetActive(false);
            }
            if (currentBoss == null && enemiesInWave.Count == 0)
            {
                bossSpawned = false;
                if (cardHandUI != null) cardHandUI.OnWaveCompleted();

                OnWaveCompleted?.Invoke(currentWave);
                OnWaveChanged?.Invoke(currentWave);

                TryOpenShopOrStartNextWave();
            }
            return;
        }

        // Normal wave slut
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

        // Boss hver 10. wave
        if (currentWave % 10 == 0)
        {
            bossHealthBarUI.SetActive(true);
            currentBoss = Instantiate(bossPrefab, bossSpawnPoint.position, Quaternion.identity);

            BossSpawner spawner = currentBoss.GetComponent<BossSpawner>();
            if (spawner != null) spawner.waveManager = this;

            bossSpawned = true;

            Slider bossSlider = bossHealthBarUI.GetComponent<Slider>();
            BossHealth bossHealth = currentBoss.GetComponent<BossHealth>();
            bossHealth.AssignHealthBar(bossSlider);

            Camera.main.GetComponent<CameraFollow>().FocusTemporarily(bossSpawnPoint.position, 2.5f);
            singleTypeIndex = -1;
            yield break;
        }

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

        int reward = wavesCleared / 5;
        if (wavesCleared >= 10 && wavesCleared % 10 == 0)
            reward += 5;

        if (reward > 0)
            MetaProgressionManager.Instance.AddShards(reward);

        // UI håndteres nu af GameOverManager
        Debug.Log($"[WaveManager] Run ended after wave {wavesCleared}. Reward: {reward} Chaos Shards.");
    }
}
