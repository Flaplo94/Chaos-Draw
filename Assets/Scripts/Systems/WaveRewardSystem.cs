using UnityEngine;

public class WaveRewardSystem : MonoBehaviour
{
    public Wallet walletRef;           // Hvis null -> Wallet.Instance
    public WaveManager waveManager;

    [Header("Wave bonus")]
    public int baseWaveGold = 10;
    public int perWaveIncrease = 2;    // +2 pr. wave

    void OnEnable()
    {
        if (waveManager != null)
            waveManager.OnWaveCompleted += HandleWaveCompleted;
    }

    void OnDisable()
    {
        if (waveManager != null)
            waveManager.OnWaveCompleted -= HandleWaveCompleted;
    }

    void HandleWaveCompleted(int waveNum)
    {
        var wallet = walletRef != null ? walletRef : Wallet.Instance;
        if (wallet == null) return;

        int reward = Mathf.Max(0, baseWaveGold + (waveNum - 1) * perWaveIncrease);
        if (reward > 0) wallet.Add(reward);
    }
}
