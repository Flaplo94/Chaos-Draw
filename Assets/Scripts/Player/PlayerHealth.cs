using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// Simpel health-komponent med ekstra liv og helper-metoder
public class PlayerHealth : MonoBehaviour
{
    public static PlayerHealth Instance;   // singleton

    [Header("Health")]
    public int maxHealth = 5;
    public int currentHealth;

    [Header("Revive")]
    public int extraLives = 0;

    [Header("UI")]
    [SerializeField] private Slider healthSlider; // drag your Slider here in Inspector

    public Action OnDeath;
    [SerializeField] private Animator playerAnimator;
    
    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;

        // check for any active shield
        var shield = FindFirstObjectByType<Shield>();
        if (shield != null)
        {
            shield.ConsumeHit();
            return; // a shield absorbed the hit
        }

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        var flash = GetComponent<HitFlash>();
        if (flash != null)
            flash.PlayFlash();

        if (healthSlider != null)
            healthSlider.value = currentHealth;

        if (currentHealth <= 0) Die();
    }

    public void Heal(int amount)
    {
        if (amount <= 0) return;

        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);

        if (healthSlider != null)
            healthSlider.value = currentHealth;
    }

    // Kravet fra artifacts: sæt alt til 1 HP
    public void ForceSetToOneHP()
    {
        maxHealth = 1;
        if (currentHealth > 1) currentHealth = 1;
        

        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }
    }

    // Ekstra liv (Spare Rib artifact)
    public void AddExtraLife(int n)
    {
        extraLives += Mathf.Max(0, n);
        
    }

    public bool TryConsumeExtraLife()
    {
        if (extraLives > 0)
        {
            extraLives--;
            currentHealth = maxHealth;

            if (healthSlider != null)
            {
                healthSlider.maxValue = maxHealth;
                healthSlider.value = currentHealth;
            }

            return true;
        }
        return false;
    }
    // --- Death + Game Over flow ---
    public void Die()
    {
        if (TryConsumeExtraLife())
            return;

        int wavesCleared = WaveManager.Instance != null ? WaveManager.Instance.CurrentWave : 0;

        // Beregn reward shards
        int reward = wavesCleared / 5;
        if (wavesCleared >= 10 && wavesCleared % 10 == 0)
            reward += 5;

        if (MetaProgressionManager.Instance != null)
            MetaProgressionManager.Instance.AddShards(reward);
        if (healthSlider != null)
        {
            healthSlider.value = 0;   // slider shows empty
            if (healthSlider.fillRect != null)
                healthSlider.fillRect.gameObject.SetActive(false);
        }
        // Play death animation instead of instantly popping UI
        if (playerAnimator != null)
        {
            playerAnimator.SetTrigger("Die");
        }

        OnDeath?.Invoke();

        // Disable player controls/collider immediately (but not the GameObject yet)
        var col = GetComponent<Collider2D>();
        if (col) col.enabled = false;
        var rb = GetComponent<Rigidbody2D>();
        if (rb) rb.simulated = false;

        // GameOverManager.TriggerGameOver will be called by animation event
        // (see below)
    }

    // This will be called from the animation event at the right frame
    public void OnDeathAnimationFinished()
    {
        int wavesCleared = WaveManager.Instance != null ? WaveManager.Instance.CurrentWave : 0;

        int reward = wavesCleared / 5;
        if (wavesCleared >= 10 && wavesCleared % 10 == 0)
            reward += 5;
        if (WaveManager.Instance != null && WaveManager.Instance.CurrentWave >= 20)
        {
            
            SceneManager.LoadScene(WaveManager.Instance.afterWave20Scene);
            return;
        }

        if (GameOverManager.Instance != null)
            GameOverManager.Instance.TriggerGameOver(wavesCleared, reward);

        // Now disable the player object entirely
        gameObject.SetActive(false);
    }

    private IEnumerator ShowGameOverDelayed(int wavesCleared, int reward)
    {
        yield return new WaitForSeconds(1.5f); // delay in seconds, tweak as you like

        if (GameOverManager.Instance != null)
            GameOverManager.Instance.TriggerGameOver(wavesCleared, reward);
        else
            Debug.LogWarning("[PlayerHealth] GameOverManager mangler!");
    }


    public void SetMaxHealthTemporary(int newMax, bool clampCurrent = true)
    {
        maxHealth = Mathf.Max(1, newMax);
        if (clampCurrent)
            currentHealth = Mathf.Min(currentHealth, maxHealth);

        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }
    }

    public void SetCurrentHealth(int hp)
    {
        currentHealth = Mathf.Clamp(hp, 0, maxHealth);

        if (healthSlider != null)
            healthSlider.value = currentHealth;

        if (currentHealth <= 0) Die();
    }
}
