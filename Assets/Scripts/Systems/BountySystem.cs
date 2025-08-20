using UnityEngine;

public class BountySystem : MonoBehaviour
{
    [Header("Refs (valgfri)")]
    public Wallet walletRef;               // Hvis null -> bruger Wallet.Instance
    public WaveManager waveManager;        // For skalering pr. wave (valgfri)

    [Header("Fallback")]
    [Tooltip("Bruges hvis en enemy ikke har EnemyBounty-komponenten")]
    public int defaultPerKill = 1;

    private int currentWave = 1;

    void OnEnable()
    {
        EnemyHealth.OnAnyEnemyDied += HandleEnemyDied;

        if (waveManager != null)
        {
            waveManager.OnWaveStarted += HandleWaveStarted;
            currentWave = Mathf.Max(1, waveManager.GetCurrentWaveNumber());
        }
    }

    void OnDisable()
    {
        EnemyHealth.OnAnyEnemyDied -= HandleEnemyDied;
        if (waveManager != null)
            waveManager.OnWaveStarted -= HandleWaveStarted;
    }

    void HandleWaveStarted(int waveNumber)
    {
        currentWave = Mathf.Max(1, waveNumber);
    }

    void HandleEnemyDied(EnemyHealth eh)
    {
        var wallet = walletRef != null ? walletRef : Wallet.Instance;
        if (wallet == null || eh == null) return;

        int amount = defaultPerKill;
        var bounty = eh.GetComponent<EnemyBounty>();
        if (bounty != null)
            amount = bounty.GetBounty(currentWave);

        if (amount > 0)
            wallet.Add(amount);
    }
}
