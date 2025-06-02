using UnityEngine;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance;

    public Transform[] spawnPoints;
    public GameObject enemy1HitPrefab;
    public GameObject enemy2HitPrefab;

    private float timer;

    void Update()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void SpawnWave(int waveNumber, int oneHitCount, int twoHitCount)
    {
        int totalEnemies = oneHitCount + twoHitCount;
        WaveManager.Instance.RegisterEnemies(totalEnemies);

        List<GameObject> spawnQueue = new List<GameObject>();

        for (int i = 0; i < oneHitCount; i++)
            spawnQueue.Add(enemy1HitPrefab);

        for (int i = 0; i < twoHitCount; i++)
            spawnQueue.Add(enemy2HitPrefab);

        // Shuffle spawnQueue
        for (int i = 0; i < spawnQueue.Count; i++)
        {
            int randIndex = Random.Range(i, spawnQueue.Count);
            GameObject temp = spawnQueue[i];
            spawnQueue[i] = spawnQueue[randIndex];
            spawnQueue[randIndex] = temp;
        }

        foreach (GameObject prefab in spawnQueue)
        {
            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            Instantiate(prefab, spawnPoint.position, Quaternion.identity);
        }
    }

    void SpawnEnemy()
    {
        Debug.Log("SPAWN");
        Vector2 spawnPosition = (Vector2)transform.position + Random.insideUnitCircle.normalized * spawnRadius;
        Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
    }
}