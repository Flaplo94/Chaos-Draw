using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class WaveManager : MonoBehaviour
{
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

    // Events til UI/TopBar
    public event Action<int> OnWaveStarted;    // kaldes når en wave starter (1-baseret)
    public event Action<int> OnWaveCompleted;  // kaldes når en wave afsluttes (1-baseret)

    public int GetCurrentWaveNumber() => currentWave;

    private int GetWeightedRandomIndex(int maxIndex)
    {
        float totalWeight = 0f;
        for (int i = 0; i < maxIndex; i++)
            totalWeight += enemySpawnWeights[i];

        float randomValue = UnityEngine.Random.value * totalWeight;
        float cumulative = 0f;
        for (int i = 0; i < maxIndex; i++)
        {
            cumulative += enemySpawnWeights[i];
            if (randomValue < cumulative)
                return i;
        }
        return maxIndex - 1;
    }

    void Awake()
    {
        Debug.Log("[Wave] Awake");
    }

    void Start()
    {
        StartCoroutine(NextWave());
    }

    void Update()
    {
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

            TryOpenShopOrStartNextWave();
        }
    }

    IEnumerator NextWave()
    {
        yield return new WaitForSeconds(timeBetweenWaves);

        currentWave++;
        if (waveText != null) waveText.text = "Wave " + currentWave;

        OnWaveStarted?.Invoke(currentWave);

        if (currentWave % 5 == 0 && unlockedEnemyTypes < enemyPrefabs.Length)
        {
            unlockedEnemyTypes++;
        }
        int maxIndex = Mathf.Min(unlockedEnemyTypes, enemyPrefabs.Length);

        // Boss hver 10. wave
        if (currentWave % 10 == 0)
        {
            bossHealthBarUI.SetActive(true);
            currentBoss = Instantiate(bossPrefab, bossSpawnPoint.position, Quaternion.identity);

            BossSpawner spawner = currentBoss.GetComponent<BossSpawner>();
            if (spawner != null)
            {
                spawner.waveManager = this;
            }

            bossSpawned = true;

            Slider bossSlider = bossHealthBarUI.GetComponent<Slider>();
            BossHealth bossHealth = currentBoss.GetComponent<BossHealth>();
            bossHealth.AssignHealthBar(bossSlider);

            Camera.main.GetComponent<CameraFollow>().FocusTemporarily(bossSpawnPoint.position, 2.5f);
            singleTypeIndex = -1;
            yield break;
        }

        int enemyCount = startEnemyCount + currentWave * 2;

        // Hver 5. wave: single type
        bool singleTypeWave = (currentWave % 5 == 0);
        if (singleTypeWave)
        {
            singleTypeIndex = GetWeightedRandomIndex(maxIndex);
        }
        else
        {
            singleTypeIndex = -1;
        }

        enemiesInWave.Clear();

        for (int i = 0; i < enemyCount; i++)
        {
            Vector2 spawnOffset;
            do
            {
                spawnOffset = UnityEngine.Random.insideUnitCircle * spawnRadius;
            }
            while (spawnOffset.magnitude < minSpawnDistance);

            Vector2 spawnPos = (Vector2)playerTransform.position + spawnOffset;

            GameObject chosenPrefab;
            if (singleTypeIndex >= 0)
                chosenPrefab = enemyPrefabs[singleTypeIndex];
            else
                chosenPrefab = enemyPrefabs[GetWeightedRandomIndex(maxIndex)];

            GameObject enemy = Instantiate(chosenPrefab, spawnPos, Quaternion.identity);
            enemiesInWave.Add(enemy);

            var eh = enemy.GetComponent<EnemyHealth>();
            if (eh != null)
                eh.OnDeath += () => enemiesInWave.Remove(enemy);
        }

        waveInProgress = true;
    }

    // ====== VIGTIG: brug += og afmeld igen, så TopBar stadig lytter ======
    private void TryOpenShopOrStartNextWave()
    {
        if (currentWave > 0 && currentWave % 5 == 0 && ShopManager.Instance != null)
        {
            // sørg for at vi ikke hænger på gamle handlers
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
        // afmeld straks, så vi ikke kalder dobbelt næste gang
        if (ShopManager.Instance != null)
            ShopManager.Instance.OnClosed -= HandleShopClosedAfterWave;

        StartCoroutine(NextWave());
    }

    // === beholdt for BossSpawner ===
    public void RegisterMinion(GameObject minion)
    {
        if (minion == null) return;

        enemiesInWave.Add(minion);

        var eh = minion.GetComponent<EnemyHealth>();
        if (eh != null)
            eh.OnDeath += () => enemiesInWave.Remove(minion);
    }
}
