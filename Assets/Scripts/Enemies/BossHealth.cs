using UnityEngine;
using UnityEngine.UI;

public class BossHealth : MonoBehaviour
{
    [SerializeField] public int maxHealth = 100;
    private int currentHealth;
    private Slider healthSlider;

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

    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;

        currentHealth -= amount;

        // Damage numbers (boss offset lidt større)
        if (DamageNumbers.Instance != null)
            DamageNumbers.Instance.Show(transform.position + Vector3.up * 0.8f, amount);

        if (healthSlider != null)
            healthSlider.value = Mathf.Max(0, currentHealth);

        if (currentHealth <= 0)
            Die();
    }

    public void Heal(int amount)
    {
        if (amount <= 0) return;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);

        if (healthSlider != null)
            healthSlider.value = currentHealth;
    }

    void Die()
    {
        Destroy(gameObject);
    }
}
