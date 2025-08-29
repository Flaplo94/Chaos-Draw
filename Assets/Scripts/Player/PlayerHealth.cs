using UnityEngine;
using UnityEngine.UI;
using System;


/// Simpel health-komponent med ekstra liv og helper-metoder
public class PlayerHealth : MonoBehaviour
{
    public static PlayerHealth Instance;   // <-- NY singleton

    [Header("Health")]
    public int maxHealth = 5;
    public int currentHealth;

    [Header("Revive")]
    public int extraLives = 0;

    public Action OnDeath; 

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;
        currentHealth -= amount;
        if (currentHealth <= 0) Die();
    }

    public void Heal(int amount)
    {
        if (amount <= 0) return;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
    }

    // Kravet fra artifacts: s�t alt til 1 HP
    public void ForceSetToOneHP()
    {
        maxHealth = 1;
        if (currentHealth > 1) currentHealth = 1;
        Debug.Log("[PlayerHealth] ForceSetToOneHP");
    }

    // Ekstra liv (Spare Rib artifact)
    public void AddExtraLife(int n)
    {
        extraLives += Mathf.Max(0, n);
        Debug.Log("[PlayerHealth] AddExtraLife: +" + n + " (total: " + extraLives + ")");
    }

    public bool TryConsumeExtraLife()
    {
        if (extraLives > 0)
        {
            extraLives--;
            currentHealth = maxHealth;
            Debug.Log("[PlayerHealth] Extra life consumed. Lives left: " + extraLives);
            return true;
        }
        return false;
    }

    // --- D�d + Game Over flow ---
    public void Die()
    {
        if (TryConsumeExtraLife())
            return;

        int wavesCleared = WaveManager.Instance != null ? WaveManager.Instance.CurrentWave : 0;

        // Beregn reward shards
        int reward = wavesCleared / 5;
        if (wavesCleared >= 10 && wavesCleared % 10 == 0)
            reward += 5;

        // Tilf�j til MetaProgression
        if (MetaProgressionManager.Instance != null)
            MetaProgressionManager.Instance.AddShards(reward);
        else
            Debug.LogWarning("[PlayerHealth] MetaProgressionManager mangler!");

        // Trigger Game Over UI
        if (GameOverManager.Instance != null)
            GameOverManager.Instance.TriggerGameOver(wavesCleared, reward);
        else
            Debug.LogWarning("[PlayerHealth] GameOverManager mangler!");

        Debug.Log($"[PlayerHealth] Dead - Game Over. Waves: {wavesCleared}, Shards: {reward}");

        // Disable player
        gameObject.SetActive(false);
    }

    public void SetMaxHealthTemporary(int newMax, bool clampCurrent = true)
    {
        maxHealth = Mathf.Max(1, newMax);
        if (clampCurrent)
            currentHealth = Mathf.Min(currentHealth, maxHealth);
    }

    public void SetCurrentHealth(int hp)
    {
        currentHealth = Mathf.Clamp(hp, 0, maxHealth);
        if (currentHealth <= 0) OnDeath();
    }
}
