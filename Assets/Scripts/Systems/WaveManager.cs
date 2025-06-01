using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class WaveManager : MonoBehaviour
{
    public GameObject[] enemyPrefabs;
    public Transform[] spawnPoints;
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
            // Pick a random spawn point from the list
            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            GameObject chosenPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
            GameObject enemy = Instantiate(chosenPrefab, spawnPoint.position, Quaternion.identity);
            enemiesInWave.Add(enemy);

            // Remove from list when enemy dies
            enemy.GetComponent<EnemyHealth>().OnDeath += () => enemiesInWave.Remove(enemy);
        }

        waveInProgress = true;
    }

    void Start()
    {
        StartCoroutine(NextWave());
    }
}
