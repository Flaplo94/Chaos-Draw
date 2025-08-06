using UnityEngine;

public class PlayerShield : MonoBehaviour
{
    private bool shieldActive = false;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    public void ActivateShield()
    {
        shieldActive = true;
        if (spriteRenderer != null)
            spriteRenderer.color = Color.blue;
    }

    public bool TryBlockDamage()
    {
        if (shieldActive)
        {
            shieldActive = false;
            if (spriteRenderer != null)
                spriteRenderer.color = originalColor;
            return true; // Damage blocked
        }
        return false; // Damage not blocked
    }
}