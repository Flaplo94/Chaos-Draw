using UnityEngine;
using UnityEngine.UI;
using static UnityEditor.Rendering.FilterWindow;

public class BossHealth : MonoBehaviour
{
    [SerializeField] public int maxHealth = 100;
    private int currentHealth;
    private Slider healthSlider;
    private HitFlash flash;
    [SerializeField] private bool flashOnLethalHit = true;
    private EnemyAnimator enemyAnimator;

    void Awake()
    {
        flash = GetComponent<HitFlash>() ?? GetComponent<HitFlash>();
        if (enemyAnimator == null)
            enemyAnimator = GetComponent<EnemyAnimator>();
    }
    void Start()
    {
        currentHealth = maxHealth;

        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = maxHealth;
        }
    }

    public void AssignHealthBar(Slider slider)
    {
        healthSlider = slider;
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }
    }

    public void TakeDamage(int amount, DamageElement element)
    {
        if (amount <= 0) return;

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
            enemyAnimator.PlayDie();
        }
    }

    void FinishDeath()
    {
        Destroy(gameObject);
    }
}
