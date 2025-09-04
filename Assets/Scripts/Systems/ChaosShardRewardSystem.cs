using UnityEngine;

public class ChaosShardRewardSystem : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private WaveManager waveManager;

    [Header("Shard Rewards")]
    [Tooltip("Shards for at klare en wave (additivt pr. wave).")]
    public int basePerWave = 1;
    [Tooltip("Ekstra shards pr. wave der er gået. (fx 0.2 = +20% mere pr. wave)")]
    public float perWaveScale = 0.1f;

    [Header("Boss Rewards")]
    [Tooltip("Ekstra shards på boss waves (10, 20, 30...).")]
    public int bossBonus = 10;

    private void OnEnable()
    {
        if (waveManager != null)
            waveManager.OnWaveCompleted += HandleWaveCompleted;
    }

    private void OnDisable()
    {
        if (waveManager != null)
            waveManager.OnWaveCompleted -= HandleWaveCompleted;
    }

    private void HandleWaveCompleted(int waveNum)
    {
        int reward = Mathf.RoundToInt(basePerWave * (1f + (waveNum - 1) * perWaveScale));

        // Boss bonus
        if (waveNum % 10 == 0)
            reward += bossBonus;

        if (reward > 0 && MetaProgressionManager.Instance != null)
        {
            MetaProgressionManager.Instance.AddShards(reward);
            Debug.Log($"[ChaosShardReward] Wave {waveNum} -> +{reward} shards");
        }
    }
}
