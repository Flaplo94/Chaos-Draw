using UnityEngine;

/// Statisk API som PlayerArtifactManager kalder. Finder service og spawner et homing projektil.
public static class BananaSpawner
{
    public static void ThrowAtRandomEnemy(Vector3 from)
    {
        var svc = Object.FindFirstObjectByType<BananaSpawnerService>();
        if (svc == null || svc.bananaProjectilePrefab == null)
        {
            Debug.LogWarning("[BananaSpawner] Mangler BananaSpawnerService eller bananaProjectilePrefab");
            return;
        }

        var target = FindRandomEnemy();
        if (target == null)
        {
            Debug.Log("[BananaSpawner] Ingen enemy fundet");
            return;
        }

        GameObject go = Object.Instantiate(svc.bananaProjectilePrefab, from, Quaternion.identity);
        var hm = go.GetComponent<HomingMissile>();
        if (hm == null) hm = go.AddComponent<HomingMissile>();
        hm.target = target.transform;
    }

    private static GameObject FindRandomEnemy()
    {
        var enemies = GameObject.FindGameObjectsWithTag("Enemy");
        if (enemies == null || enemies.Length == 0) return null;
        int i = Random.Range(0, enemies.Length);
        return enemies[i];
    }
}
