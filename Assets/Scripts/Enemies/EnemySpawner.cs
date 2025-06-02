using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public float spawnInterval = 2f;
    public float spawnRadius = 5f;

    private float timer;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            timer = 0f;
            SpawnEnemy();
        }
    }

    void SpawnEnemy()
    {
        Debug.Log("SPAWN");
        Vector2 spawnPosition = (Vector2)transform.position + Random.insideUnitCircle.normalized * spawnRadius;
        Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
    }
}