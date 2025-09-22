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
        if (PlayerInventory.Instance != null && PlayerArtifactManager.Instance != null)
        {
            foreach (var a in PlayerInventory.Instance.artifacts)
                PlayerArtifactManager.Instance.AddArtifact(a);
        }
    }

    public void Apply(ArtifactData a)
    {
        if (a == null || string.IsNullOrWhiteSpace(a.internalID)) return;

        if (PlayerArtifactManager.Instance != null &&
        PlayerArtifactManager.Instance.HasArtifact(a.internalID))
            return;

        // Optional duplicate guard (per-session), helps if something calls Apply twice:
        string key = Normalize(a.internalID);
        if (applied.Contains(key)) return;
        applied.Add(key);

        // Unified effect path lives here:
        if (PlayerArtifactManager.Instance != null)
        {
            PlayerArtifactManager.Instance.AddArtifact(a);
        }
        else
        {
            Debug.LogWarning("[ArtifactSystem] PlayerArtifactManager not found; cannot apply " + a.internalID);
        }
    }

    static string Normalize(string s)
    {
        s = s.Trim().ToLowerInvariant();
        s = s.Replace(" ", "").Replace("_", "").Replace("-", "");
        return s;
    }
}
