using System.Collections.Generic;
using UnityEngine;

public class ArtifactSystem : MonoBehaviour
{
    public static ArtifactSystem Instance;

    [Header("Refs (assign on Player)")]
    public PlayerThrowing throwingRef;
    public PlayerHealth healthRef;
    public PlayerMovement movementRef;
    public PlayerStats statsRef;

    // Track which artifact IDs have been applied
    private readonly HashSet<string> applied = new HashSet<string>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        // Apply already-owned artifacts at scene start (e.g., after load)
        if (PlayerInventory.Instance != null)
        {
            foreach (var a in PlayerInventory.Instance.artifacts)
                Apply(a);
        }
        PushRuntime();
    }

    public void Apply(ArtifactData a)
    {
        if (a == null || string.IsNullOrWhiteSpace(a.internalID)) return;

        string id = Normalize(a.internalID);
        if (applied.Contains(id)) return; // extra safety
        applied.Add(id);

        switch (id)
        {
            // === Demo artifacts ===

            // Charcoal: +40% Fire damage
            case "charcoal":
                if (statsRef != null) statsRef.fireDamageMult *= 1.40f;
                Debug.Log("[Artifact] Charcoal: +40% Fire damage");
                break;

            // Gamers Cap: +10% to all stats
            // Interpreted as: global damage, move speed, throw cadence (fire rate), projectile speed
            case "gamerscap":
                if (statsRef != null) statsRef.damageMult *= 1.10f; // +10% global dmg
                if (movementRef != null) movementRef.moveSpeed *= 1.10f; // +10% move speed
                if (throwingRef != null)
                {
                    throwingRef.fireRateMult *= 1.10f; // faster throws (lower cooldown)
                    throwingRef.projectileSpeedMult *= 1.10f; // faster projectiles
                }
                Debug.Log("[Artifact] Gamers Cap: +10% to all stats");
                break;

            // Glass Cannon: set HP to 1 and +300% damage (x4 total)
            case "glasscannon":
                if (healthRef != null) healthRef.ForceSetToOneHP();
                if (statsRef != null) statsRef.damageMult *= 4.0f;
                Debug.Log("[Artifact] Glass Cannon: HP set to 1, +300% damage");
                break;

            default:
                Debug.Log("[ArtifactSystem] Unknown artifact id: '" + a.internalID + "' (no runtime effect)");
                break;
        }

        PushRuntime();
    }

    void PushRuntime()
    {
        // Currently all effects push directly on apply.
        // Keep this method for future aggregation if needed.
    }

    static string Normalize(string s)
    {
        s = s.Trim().ToLowerInvariant();
        s = s.Replace(" ", "").Replace("_", "").Replace("-", "");
        return s;
    }
}
