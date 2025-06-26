using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class WaveManager : MonoBehaviour
{
    [SerializeField] private Transform playerTransform;
    [SerializeField] private float minSpawnDistance = 3f;
    [SerializeField] private float spawnRadius = 10f;
    public GameObject[] enemyPrefabs;
    [Header("Spawn weights must match enemyPrefabs order!")]
    [SerializeField] private float[] enemySpawnWeights = new float[] { 1f, 1f, 0.3f, 0.1f }; // Example: melee, melee, ranged, healer
    public GameObject bossPrefab;
    public Transform bossSpawnPoint;
    public GameObject bossHealthBarUI;
    private bool bossSpawned = false;
    private GameObject currentBoss;
    public int startEnemyCount = 3;
    public float timeBetweenWaves = 2f;
    private int singleTypeIndex = -1;
    private int unlockedEnemyTypes = 2; // Start with 2 types unlocked

    private int currentWave = 0;
    private List<GameObject> enemiesInWave = new List<GameObject>();
    private bool waveInProgress = false;
    public TextMeshProUGUI waveText;

    // --- Weighted random selection helper ---
    private int GetWeightedRandomIndex(int maxIndex)
    {
        float totalWeight = 0f;
        for (int i = 0; i < maxIndex; i++)
            totalWeight += enemySpawnWeights[i];

        float randomValue = Random.value * totalWeight;
        float cumulative = 0f;
        for (int i = 0; i < maxIndex; i++)
        {
            cumulative += enemySpawnWeights[i];
            if (randomValue < cumulative)
                return i;
        }
        return maxIndex - 1; // fallback
    }
    // ----------------------------------------

    void Update()
    {
        if (bossSpawned)
        {
            if (currentBoss == null)
            {
                bossHealthBarUI.SetActive(false); // Hide health bar immediately when boss dies
            }
            if (currentBoss == null && enemiesInWave.Count == 0)
            {
                bossSpawned = false;
                StartCoroutine(NextWave()); // resume normal waves
            }
            return; // don't spawn waves while boss or minions are alive
        }


        if (waveInProgress && enemiesInWave.Count == 0)
        {
            waveInProgress = false;
            StartCoroutine(NextWave());
        }
    }

    IEnumerator NextWave()
    {
        yield return new WaitForSeconds(timeBetweenWaves);

        currentWave++;
        waveText.text = "Wave " + currentWave;

        // Unlock a new enemy type every 5th wave, up to all types
        if (currentWave % 5 == 0 && unlockedEnemyTypes < enemyPrefabs.Length)
        {
            unlockedEnemyTypes++;
        }
        int maxIndex = Mathf.Min(unlockedEnemyTypes, enemyPrefabs.Length);

        // Boss wave every 10th wave
        if (currentWave % 10 == 0)
        {
            bossHealthBarUI.SetActive(true);
            currentBoss = Instantiate(bossPrefab, bossSpawnPoint.position, Quaternion.identity);

            // Set the waveManager reference on the boss's BossSpawner
            BossSpawner spawner = currentBoss.GetComponent<BossSpawner>();
            if (spawner != null)
            {
                spawner.waveManager = this;
            }

            bossSpawned = true;

            // Assign health bar
            Slider bossSlider = bossHealthBarUI.GetComponent<Slider>();
            BossHealth bossHealth = currentBoss.GetComponent<BossHealth>();
            bossHealth.AssignHealthBar(bossSlider);

            Camera.main.GetComponent<CameraFollow>().FocusTemporarily(bossSpawnPoint.position, 2.5f);
            singleTypeIndex = -1; // Reset for after boss
            yield break; // skip regular enemies this wave
        }

        int enemyCount = startEnemyCount + currentWave * 2;

        // Every 5th wave, pick a single random enemy type (from unlocked, weighted)
        bool singleTypeWave = (currentWave % 5 == 0);

        if (singleTypeWave)
        {
            singleTypeIndex = GetWeightedRandomIndex(maxIndex);
        }
        else
        {
            singleTypeIndex = -1;
        }

        for (int i = 0; i < enemyCount; i++)
        {
            Vector2 spawnOffset;
            do
            {
                spawnOffset = Random.insideUnitCircle * spawnRadius;
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

            enemy.GetComponent<EnemyHealth>().OnDeath += () => enemiesInWave.Remove(enemy);
        }

        waveInProgress = true;
    }

    void Start()
    {
        StartCoroutine(NextWave());
    }
    public void RegisterMinion(GameObject minion)
    {
        enemiesInWave.Add(minion);
        minion.GetComponent<EnemyHealth>().OnDeath += () => enemiesInWave.Remove(minion);
    }
}
