using UnityEngine;

/// Attach this to a prefab and set that prefab as Ability.effectPrefab.
/// On cast, it heals the player instantly (flat amount), then destroys itself.
public class Healing : MonoBehaviour, IAbilityBehavior
{
    [Header("Healing")]
    public int healAmount = 5; // base at Common

    [Header("VFX / SFX (optional)")]
    public GameObject healVfxPrefab;
    public float vfxLifetime = 1.25f;
    public AudioClip healSfx;
    public AudioSource audioSource;

    public bool Initialize(Vector2 dir, Rarity rarity)
    {
        // RARITY TWEAKS — same style as your Fireball:
        switch (rarity)
        {
            case Rarity.Uncommon: healAmount += 2; break;
            case Rarity.Rare: healAmount += 5; break;
            case Rarity.Epic: healAmount += 10; break;
            case Rarity.Legendary: healAmount += 20; break;
        }

        // Heal the player
        var playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth == null)
        {
            Debug.LogWarning("[HealSelfFlat] No PlayerHealth found in scene.");
            Destroy(gameObject);
            return false;
        }

        // Your PlayerHealth is expected to clamp to max internally.
        // If your API name differs, change this line.
        playerHealth.Heal(healAmount);

        // Optional feedback
        if (healVfxPrefab != null)
        {
            var vfx = Instantiate(healVfxPrefab, playerHealth.transform.position, Quaternion.identity);
            if (vfxLifetime > 0f) Destroy(vfx, vfxLifetime);
        }
        if (healSfx != null)
        {
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = playerHealth.GetComponent<AudioSource>();
            if (audioSource != null) audioSource.PlayOneShot(healSfx);
        }

        Destroy(gameObject);
        return true;
    }
}
