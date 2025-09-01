using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] public int maxHealth = 100;
    private int currentHealth;
    private Slider healthSlider;

    [Header("Visuals")]
    private HitFlash flash;
    [SerializeField] private bool flashOnLethalHit = true;
    private EnemyAnimator enemyAnimator;

    [Header("UI")]
    private TextMeshProUGUI bossNameText;   // assigned at runtime
    [SerializeField] private string displayName; // optional override name

    private bool isDead = false; // NEW FLAG

    void Awake()
    {
        flash = GetComponent<HitFlash>() ?? GetComponent<HitFlash>();
        if (enemyAnimator == null)
            enemyAnimator = GetComponent<EnemyAnimator>();
    }

    void Start()
    {
        currentHealth = maxHealth;

        // Workaround: force one "hit" at spawn if needed (prevents flicker bug)
        TakeDamage(1, DamageElement.Physical);

        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = maxHealth;
        }

        if (bossNameText != null)
        {
            bossNameText.text = string.IsNullOrEmpty(displayName)
                ? gameObject.name.Replace("(Clone)", "")
                : displayName;
        }
    }

    // Assign slider + text from WaveManager
    public void AssignHealthBar(Slider slider, TextMeshProUGUI nameText)
    {
        healthSlider = slider;
        bossNameText = nameText;

        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }

        if (bossNameText != null)
        {
            bossNameText.text = string.IsNullOrEmpty(displayName)
                ? gameObject.name.Replace("(Clone)", "")
                : displayName;

            // optional: enforce black text
            bossNameText.color = Color.black;
        }
    }

    public void TakeDamage(int amount, DamageElement element)
    {
        if (amount <= 0) return;
        if (isDead) return; // ignore hits after death

        currentHealth -= amount;

        // === Damage Numbers ===
        if (DamageNumbers.Instance != null)
            DamageNumbers.Instance.Show(transform.position, amount, element);

        if (flashOnLethalHit || currentHealth > 0)
            flash?.PlayFlash();

        if (healthSlider != null)
            healthSlider.value = currentHealth;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        // Disable ALL colliders so bullets stop colliding
        foreach (var col in GetComponentsInChildren<Collider2D>())
            col.enabled = false;

        enemyAnimator?.PlayDie();
    }

    // Called by animation event at the end of the death animation
    void FinishDeath()
    {
        if (WaveManager.Instance != null && WaveManager.Instance.bossHealthBarUI != null)
            WaveManager.Instance.bossHealthBarUI.SetActive(false);

        Destroy(gameObject);
    }
}
