using UnityEngine;

/// Statisk API til at spawne en ost-orbiter
public static class OrbiterSystem
{
    public static void SpawnCheese(Transform owner)
    {
        var svc = Object.FindFirstObjectByType<OrbiterSystemService>();
        if (svc == null || svc.cheeseOrbiterPrefab == null)
        {
            Debug.LogWarning("[OrbiterSystem] Mangler OrbiterSystemService eller cheeseOrbiterPrefab");
            return;
        }

        GameObject go = Object.Instantiate(svc.cheeseOrbiterPrefab, owner.position, Quaternion.identity);
        var orb = go.GetComponent<SimpleOrbiter>();
        if (orb == null) orb = go.AddComponent<SimpleOrbiter>();
        orb.owner = owner;
    }
}
