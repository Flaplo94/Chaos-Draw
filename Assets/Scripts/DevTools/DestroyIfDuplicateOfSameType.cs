using UnityEngine;

/// <summary>
/// Minimal guard: Hvis der findes mere end én instans af denne komponenttype i scenen,
/// destruerer den sig selv. Ændrer ikke andre systemer og kalder ikke DontDestroyOnLoad.
/// Læg den på samme GameObject som din singleton-komponent (fx PlayerBuffManager).
/// </summary>
public class DestroyIfDuplicateOfSameType : MonoBehaviour
{
    void Awake()
    {
        var t = GetType();
        var all = FindObjectsOfType(t, true); // også inaktive
        if (all.Length > 1)
        {
            Debug.LogWarning($"[DuplicateGuard] Destroying duplicate {t.Name} on '{name}'. Found {all.Length} instances.");
            Destroy(gameObject);
        }
    }
}
