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
    public GameObject bossPrefab;
    public Transform bossSpawnPoint;
    public GameObject bossHealthBarUI;
    private bool bossSpawned = false;
    private GameObject currentBoss;
    public int startEnemyCount = 3;
    public float timeBetweenWaves = 2f;

    private int currentWave = 0;
    private List<GameObject> enemiesInWave = new List<GameObject>();
    private bool waveInProgress = false;
    public TextMeshProUGUI waveText;

    void Update()
    {
        if (bossSpawned)
        {
            if (currentBoss == null)
            {
                bossSpawned = false;
                bossHealthBarUI.SetActive(false);
                StartCoroutine(NextWave()); // resume normal waves
            }
            return; // don't spawn waves while boss is alive
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

        if (currentWave % 10 == 0)
        {
            bossHealthBarUI.SetActive(true);
            currentBoss = Instantiate(bossPrefab, bossSpawnPoint.position, Quaternion.identity);
            bossSpawned = true;

            // Assign health bar
            Slider bossSlider = bossHealthBarUI.GetComponent<Slider>();
            BossHealth bossHealth = currentBoss.GetComponent<BossHealth>();
            bossHealth.AssignHealthBar(bossSlider);

            yield break; // skip regular enemies this wave
        }

        int enemyCount = startEnemyCount + currentWave * 2;

        for (int i = 0; i < enemyCount; i++)
        {
            Vector2 spawnOffset;
            do
            {
                spawnOffset = Random.insideUnitCircle * spawnRadius;
            }
            while (spawnOffset.magnitude < minSpawnDistance);

            Vector2 spawnPos = (Vector2)playerTransform.position + spawnOffset;

            GameObject chosenPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
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
}
