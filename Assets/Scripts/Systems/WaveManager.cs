using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    public GameObject enemyType1; // 1-hit fjende
    public GameObject enemyType2; // 2-hit fjende
    public Transform[] spawnPoints;
    public int enemiesPerWave = 5;
    public float timeBetweenSpawns = 0.5f;

    private int currentWave = 0;
    private int enemiesAlive = 0;
    private bool isSpawning = false;

    void Start()
    {
        StartCoroutine(SpawnWave());
    }

    void Update()
    {
        if (!isSpawning && enemiesAlive == 0)
        {
            currentWave++;
            Time.timeScale = 0f;

            //var ui = FindAnyObjectByType<CardChoiceUI>();
            //if (ui != null)
            //{
            //    if (currentWave % 3 == 0)
            //        ui.ShowCardChoices(OnAbilityChosen);
            //    else
            //        ui.ShowCardChoices(OnBuffChosen);
            //}
        }
    }

    IEnumerator SpawnWave()
    {
        isSpawning = true;

        for (int i = 0; i < enemiesPerWave; i++)
        {
            GameObject prefabToSpawn = (Random.value > 0.5f) ? enemyType1 : enemyType2;
            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

            Instantiate(prefabToSpawn, spawnPoint.position, Quaternion.identity);
            enemiesAlive++;
            yield return new WaitForSeconds(timeBetweenSpawns);
        }

        isSpawning = false;
    }

    public void EnemyDied()
    {
        enemiesAlive--;
    }

    void OnBuffChosen()
    {
        ResumeGame();
    }

    void OnAbilityChosen()
    {
        ResumeGame();
    }

    void ResumeGame()
    {
        Time.timeScale = 1f;
        StartCoroutine(SpawnWave());
    }
}
