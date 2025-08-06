using UnityEngine;

public class BossSpawner : MonoBehaviour
{
    public GameObject[] minionPrefabs;
    public float spawnInterval = 3f;
    public float spawnRadius = 5f;
    public WaveManager waveManager; // Set this from WaveManager

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
            GameObject minion = Instantiate(prefab, spawnPos, Quaternion.identity);
            if (waveManager != null)
            {
                waveManager.RegisterMinion(minion);
            }
        }
    }
}
