using UnityEngine;

public class Shield : MonoBehaviour
{
    public float duration = 0f; // Not used for one-hit, but can be extended
    private bool shieldApplied = false;

    void Start()
    {
        // Find the player and activate their shield
        PlayerShield playerShield = FindFirstObjectByType<PlayerShield>();
        if (playerShield != null && !shieldApplied)
        {
            playerShield.ActivateShield();
            shieldApplied = true;
        }
        // Destroy the shield effect object immediately (no need to stay in scene)
        Destroy(gameObject);
    }
}