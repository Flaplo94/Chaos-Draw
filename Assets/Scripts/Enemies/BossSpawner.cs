using UnityEngine;

public class BossSpawner : MonoBehaviour
{
    public GameObject[] minionPrefabs;
    public float spawnInterval = 3f;
    public float spawnRadius = 5f;
    public float safeDistanceFromPlayer = 3f; // minimum distance from player

    public WaveManager waveManager; // Set this from WaveManager

    private float spawnTimer = 0f;
    private Transform player;

    void Start()
    {
        // Cache player reference
        PlayerHealth ph = FindFirstObjectByType<PlayerHealth>();
        if (ph != null) player = ph.transform;
    }

    void Update()
    {
        spawnTimer -= Time.deltaTime;

        if (spawnTimer <= 0f)
        {
            spawnTimer = spawnInterval;

            Vector2 spawnPos = GetSafeSpawnPosition();

            if (spawnPos != Vector2.zero) // found valid spot
            {
                GameObject prefab = minionPrefabs[Random.Range(0, minionPrefabs.Length)];
                GameObject minion = Instantiate(prefab, spawnPos, Quaternion.identity);
                if (waveManager != null)
                {
                    waveManager.RegisterMinion(minion);
                }
            }
        }
    }

    private Vector2 GetSafeSpawnPosition()
    {
        const int maxAttempts = 20;

        for (int i = 0; i < maxAttempts; i++)
        {
            Vector2 spawnOffset = Random.insideUnitCircle.normalized * spawnRadius;
            Vector2 spawnPos = (Vector2)transform.position + spawnOffset;

            if (player == null || Vector2.Distance(spawnPos, player.position) >= safeDistanceFromPlayer)
            {
                return spawnPos;
            }
        }

        // fallback if no valid spot found
        return Vector2.zero;
    }
}
