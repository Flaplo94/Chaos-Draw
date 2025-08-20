using UnityEngine;

/// Simpel health-komponent med ekstra liv og helper-metoder
public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 5;
    public int currentHealth = 5;

    [Header("Revive")]
    public int extraLives = 0;

    void Start()
    {
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;
        currentHealth -= amount;
        if (currentHealth <= 0) OnDeath();
    }

    public void Heal(int amount)
    {
        if (amount <= 0) return;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
    }

    // Kravet fra artifacts: saet alt til 1 HP
    public void ForceSetToOneHP()
    {
        maxHealth = 1;
        if (currentHealth > 1) currentHealth = 1;
        Debug.Log("[PlayerHealth] ForceSetToOneHP");
        // TODO: opdater UI hvis noedvendigt
    }

    // Ekstra liv (Spare Rib)
    public void AddExtraLife(int n)
    {
        extraLives += Mathf.Max(0, n);
        Debug.Log("[PlayerHealth] AddExtraLife: +" + n + " (total: " + extraLives + ")");
        // TODO: UI badge/ikon
    }

    public bool TryConsumeExtraLife()
    {
        if (extraLives > 0)
        {
            extraLives--;
            currentHealth = maxHealth;
            Debug.Log("[PlayerHealth] Extra life consumed. Lives left: " + extraLives);
            // TODO: kort invuln, fx 1s
            return true;
        }
        return false;
    }

    // Kald denne i din death-flow
    public void OnDeath()
    {
        if (TryConsumeExtraLife())
        {
            // Afbryd doed
            return;
        }
        Debug.Log("[PlayerHealth] Dead - Game Over flow her");
        // TODO: Game Over
    }
}
