using System;
using System.Collections;
using UnityEngine;
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

    // Tracking for MaxHP-buffs & regen
    private int baseMaxHealth;
    private Coroutine regenRoutine;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // Basis fra prefab/scene
        baseMaxHealth = maxHealth;

        // Start altid fuldt HP på første frame
        currentHealth = maxHealth;

        // Lyt til buff-ændringer
        if (PlayerBuffManager.Instance != null)
            PlayerBuffManager.Instance.OnValuesChanged += OnBuffValuesChanged;

        // Anvend MaxHP-mult fra buffs og REFILL til fuldt HP ved opstart
        UpdateMaxHPFromBuffs(refill: true);

        // Start simpel 5-sek regen-loop
        StartHPRegen();

        UpdateUI();
    }

    private void OnDestroy()
    {
        if (PlayerBuffManager.Instance != null)
            PlayerBuffManager.Instance.OnValuesChanged -= OnBuffValuesChanged;
    }

    private void OnBuffValuesChanged()
    {
        // Under run vil vi ikke give gratis heal, så refill=false
        UpdateMaxHPFromBuffs(refill: false);
    }

    /// <summary>
    /// Opdaterer maxHealth ud fra buffs. 
    /// refill=true: sæt currentHealth til fuldt (bruges kun ved opstart).
    /// refill=false: bevar nuværende HP proportionelt (eller clamp ned).
    /// </summary>
    private void UpdateMaxHPFromBuffs(bool refill)
    {
        float mult = 1f;
        if (PlayerBuffManager.Instance != null)
            mult = Mathf.Max(0.1f, PlayerBuffManager.Instance.GetMaxHpMult());

        int newMax = Mathf.Max(1, Mathf.RoundToInt(baseMaxHealth * mult));

        if (refill)
        {
            // Ved opstart: altid fuldt HP
            maxHealth = newMax;
            currentHealth = newMax;
        }
        else
        {
            // Mid-run: bevar procentuel HP, undgå gratis heal
            float ratio = (maxHealth > 0) ? (float)currentHealth / maxHealth : 1f;
            maxHealth = newMax;
            currentHealth = Mathf.Clamp(Mathf.RoundToInt(ratio * maxHealth), 0, maxHealth);
        }

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

        // Armor reduktion
        if (PlayerBuffManager.Instance != null)
        {
            float red = Mathf.Clamp01(PlayerBuffManager.Instance.GetArmorReductionPct());
            float mult = 1f - red; // 0.10 -> 90% damage
            amount = Mathf.Max(0, Mathf.RoundToInt(amount * mult));
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

        if (healthFill != null) healthFill.fillAmount = 0;
        if (hpEffect != null) hpEffect.fillAmount = 0;

        if (playerAnimator != null)
            playerAnimator.SetTrigger("Die");

        OnDeath?.Invoke();

        var col = GetComponent<Collider2D>();
        if (col) col.enabled = false;
        var rb = GetComponent<Rigidbody2D>();
        if (rb) rb.simulated = false;
    }

    private void UpdateUI()
    {
        float ratio = (float)currentHealth / Mathf.Max(1, maxHealth);

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

    // --- Simpel regen pr. 5 sek baseret på PBM:
    private void StartHPRegen()
    {
        if (regenRoutine != null) StopCoroutine(regenRoutine);
        regenRoutine = StartCoroutine(RegenLoop());
    }

    private IEnumerator RegenLoop()
    {
        var wait = new WaitForSeconds(5f);
        while (true)
        {
            yield return wait;
            if (PlayerBuffManager.Instance != null)
            {
                float per5 = PlayerBuffManager.Instance.GetHPRegenPer5Sec();
                if (per5 > 0f && currentHealth > 0 && currentHealth < maxHealth)
                {
                    int heal = Mathf.RoundToInt(per5);
                    Heal(heal);
                }
            }
        }
    }
}
