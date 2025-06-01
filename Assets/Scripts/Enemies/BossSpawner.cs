using UnityEngine;

public class BossSpawner : MonoBehaviour
{
    public GameObject[] minionPrefabs;
    public float spawnInterval = 3f;
    public float spawnRadius = 5f;

    private float spawnTimer = 0f;

    void Update()
    {
        spawnTimer -= Time.deltaTime;

        if (spawnTimer <= 0f)
        {
            spawnTimer = spawnInterval;

            Vector2 spawnOffset = Random.insideUnitCircle.normalized * spawnRadius;
            Vector2 spawnPos = (Vector2)transform.position + spawnOffset;

            GameObject prefab = minionPrefabs[Random.Range(0, minionPrefabs.Length)];
            Instantiate(prefab, spawnPos, Quaternion.identity);
        }
    }
}