using UnityEngine;
using UnityEngine.UI;

public class BossHealth : MonoBehaviour
{
    public int maxHealth = 100;
    private int currentHealth;
    private Slider healthSlider;
    private HitFlash flash;
    [SerializeField] private bool flashOnLethalHit = true;

    void Awake()
    {
        flash = GetComponent<HitFlash>() ?? GetComponentInChildren<HitFlash>(true);
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
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;

        if (flashOnLethalHit || currentHealth > 0)
            flash?.PlayFlash();

        if (healthSlider != null)
            healthSlider.value = currentHealth;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Destroy(gameObject);
    }
}