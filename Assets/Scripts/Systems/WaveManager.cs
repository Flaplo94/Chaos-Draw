using UnityEngine;
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
    private List<GameObject> enemiesInWave = new List<GameObject>();
    private bool waveInProgress = false;
    public TextMeshProUGUI waveText;

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
        return maxIndex - 1; 
    }
    

    void Update()
    {
        if (bossSpawned)
        {
            if (currentBoss == null)
            {
                bossHealthBarUI.SetActive(false);
            }
            if (currentBoss == null && enemiesInWave.Count == 0)
            {
                bossSpawned = false;
                cardHandUI.OnWaveCompleted();
                StartCoroutine(NextWave());
            }
            return;
        }


        if (waveInProgress && enemiesInWave.Count == 0)
        {
            waveInProgress = false;
            CardHandUI cardUI = FindFirstObjectByType<CardHandUI>();
            if (cardUI != null)
            {
                cardUI.OnWaveCompleted();
            }
            StartCoroutine(NextWave());
        }
    }

    IEnumerator NextWave()
    {
        yield return new WaitForSeconds(timeBetweenWaves);

        currentWave++;
        waveText.text = "Wave " + currentWave;

        
        if (currentWave % 5 == 0 && unlockedEnemyTypes < enemyPrefabs.Length)
        {
            unlockedEnemyTypes++;
        }
        int maxIndex = Mathf.Min(unlockedEnemyTypes, enemyPrefabs.Length);

        
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
