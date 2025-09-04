using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class PlayerHealth : MonoBehaviour
{
    public static PlayerHealth Instance;

    [Header("Health Settings")]
    public int maxHealth = 50;
    public int currentHealth;

    [Header("Revive")]
    public int extraLives = 0;

    [Header("UI References")]
    [SerializeField] private Image healthFill;   // Rød fyld
    [SerializeField] private Image hpEffect;     // Effekt ovenpå fyld
    [SerializeField] private TMP_Text hpText;    // Tekst current/max
    [SerializeField] private RectTransform edgeVfx; // Lys-streg

    [Header("Other")]
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
        UpdateUI();
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;

        var shield = FindFirstObjectByType<Shield>();
        if (shield != null)
        {
            shield.ConsumeHit();
            return;
        }

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        var flash = GetComponent<HitFlash>();
        if (flash != null) flash.PlayFlash();

        UpdateUI();

        if (currentHealth <= 0) Die();
    }

    public void Heal(int amount)
    {
        if (amount <= 0) return;

        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        UpdateUI();
    }

    public void ForceSetToOneHP()
    {
        maxHealth = 1;
        currentHealth = 1;
        Debug.Log("[PlayerHealth] ForceSetToOneHP");
        UpdateUI();
    }

    public void AddExtraLife(int n)
    {
        extraLives += Mathf.Max(0, n);
        Debug.Log("[PlayerHealth] AddExtraLife: +" + n);
    }

    public bool TryConsumeExtraLife()
    {
        if (extraLives > 0)
        {
            extraLives--;
            currentHealth = maxHealth;
            UpdateUI();
            return true;
        }
        return false;
    }

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

        if (healthFill != null) healthFill.fillAmount = 0;
        if (hpEffect != null) hpEffect.fillAmount = 0;

        if (playerAnimator != null)
            playerAnimator.SetTrigger("Die");

        Debug.Log($"[PlayerHealth] Dead - Game Over. Waves: {wavesCleared}, Shards: {reward}");

        OnDeath?.Invoke();

        var col = GetComponent<Collider2D>();
        if (col) col.enabled = false;
        var rb = GetComponent<Rigidbody2D>();
        if (rb) rb.simulated = false;
    }

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

        gameObject.SetActive(false);
    }

    private IEnumerator ShowGameOverDelayed(int wavesCleared, int reward)
    {
        yield return new WaitForSeconds(1.5f);

        if (GameOverManager.Instance != null)
            GameOverManager.Instance.TriggerGameOver(wavesCleared, reward);
        else
            Debug.LogWarning("[PlayerHealth] GameOverManager mangler!");
    }

    public void SetMaxHealthTemporary(int newMax, bool clampCurrent = true)
    {
        maxHealth = Mathf.Max(1, newMax);
        if (clampCurrent) currentHealth = Mathf.Min(currentHealth, maxHealth);
        UpdateUI();
    }

    public void SetCurrentHealth(int hp)
    {
        currentHealth = Mathf.Clamp(hp, 0, maxHealth);
        UpdateUI();
        if (currentHealth <= 0) Die();
    }

    private void UpdateUI()
    {
        float ratio = (float)currentHealth / maxHealth;

        if (healthFill != null)
            healthFill.fillAmount = ratio;

        if (hpEffect != null)
            hpEffect.fillAmount = ratio;

        if (hpText != null)
            hpText.text = $"{currentHealth}/{maxHealth}";

        if (edgeVfx != null && healthFill != null)
        {
            float height = ((RectTransform)healthFill.transform).rect.height;
            edgeVfx.anchoredPosition = new Vector2(
                edgeVfx.anchoredPosition.x,
                -height / 2f + height * ratio
            );
        }
    }
}
