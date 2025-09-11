using System.Collections.Generic;
using UnityEngine;

public class ArtifactSystem : MonoBehaviour
{
    public static ArtifactSystem Instance;

    [Header("Refs (assign on Player)")]
    public PlayerThrowing throwingRef;
    public PlayerHealth healthRef;

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
    }

    public void Apply(ArtifactData a)
    {
        if (a == null || string.IsNullOrWhiteSpace(a.internalID)) return;

        string id = Normalize(a.internalID);
        if (applied.Contains(id)) return;
        applied.Add(id);

        switch (id)
        {
            // === Demo artifacts ===

            case "charcoal":
                if (PlayerBuffManager.Instance != null)
                    PlayerBuffManager.Instance.AddRuntimeBonus(BuffData.BuffType.FireDamage, 0.40f); // +40%
                Debug.Log("[Artifact] Charcoal applied: +40% Fire damage");
                break;

            case "gamerscap":
                if (PlayerBuffManager.Instance != null)
                {
                    PlayerBuffManager.Instance.AddRuntimeBonus(BuffData.BuffType.Damage, 0.10f);       // +10% global dmg
                    PlayerBuffManager.Instance.AddRuntimeBonus(BuffData.BuffType.Speed, 0.10f);        // +10% move speed
                    PlayerBuffManager.Instance.AddRuntimeBonus(BuffData.BuffType.AttackSpeed, 0.10f);  // +10% attack speed
                    PlayerBuffManager.Instance.AddRuntimeBonus(BuffData.BuffType.FireDamage, 0.10f);
                    PlayerBuffManager.Instance.AddRuntimeBonus(BuffData.BuffType.ThunderDamage, 0.10f);
                    PlayerBuffManager.Instance.AddRuntimeBonus(BuffData.BuffType.BurnDamage, 0.10f);
                }
                if (throwingRef != null)
                {
                    throwingRef.projectileSpeedMult *= 1.10f; // stadig lokalt for projektiler
                }
                Debug.Log("[Artifact] Gamer's Cap applied: +10% all stats");
                break;

            case "glasscannon":
                if (healthRef != null) healthRef.ForceSetToOneHP();
                if (PlayerBuffManager.Instance != null)
                    PlayerBuffManager.Instance.AddRuntimeBonus(BuffData.BuffType.Damage, 3.0f); // +300%
                Debug.Log("[Artifact] Glass Cannon applied: HP=1, +300% dmg");
                break;

            default:
                Debug.Log("[ArtifactSystem] Unknown artifact id: '" + a.internalID + "' (no runtime effect)");
                break;
        }
    }

    static string Normalize(string s)
    {
        s = s.Trim().ToLowerInvariant();
        s = s.Replace(" ", "").Replace("_", "").Replace("-", "");
        return s;
    }
}
