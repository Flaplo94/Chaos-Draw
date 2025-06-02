using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance;

    public Transform[] spawnPoints;
    public GameObject enemyPrefab1Hit;
    public GameObject enemyPrefab2Hit;

    private void Awake()
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

        for (int i = 0; i < totalEnemies; i++)
        {
            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            GameObject prefab = (i < oneHitCount) ? enemyPrefab1Hit : enemyPrefab2Hit;

            Instantiate(prefab, spawnPoint.position, Quaternion.identity);
        }
    }
}
