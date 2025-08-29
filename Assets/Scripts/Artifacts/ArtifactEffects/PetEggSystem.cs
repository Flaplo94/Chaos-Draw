using UnityEngine;
using System.Collections;

/// Artifact: Chicken Overlord Egg. Efter 100 kills spawner et pet.
public static class PetEggSystem
{
    private static bool s_enabled;

    public static void Enable(MonoBehaviour host)
    {
        if (s_enabled) return;
        s_enabled = true;
        host.StartCoroutine(WaitAndSpawn(host.transform));
        Debug.Log("[PetEgg] Enabled");
    }

    private static IEnumerator WaitAndSpawn(Transform owner)
    {
        int start = KillCounter.TotalKills;
        int target = start + 100;

        while (KillCounter.TotalKills < target)
            yield return null;

        var svc = Object.FindFirstObjectByType<PetSystemService>();
        if (svc == null || svc.chickenPetPrefab == null)
        {
            Debug.LogWarning("[PetEgg] Mangler PetSystemService eller chickenPetPrefab");
            yield break;
        }

        Object.Instantiate(svc.chickenPetPrefab, owner.position, Quaternion.identity);
        Debug.Log("[PetEgg] Chicken pet spawned");
    }
}
