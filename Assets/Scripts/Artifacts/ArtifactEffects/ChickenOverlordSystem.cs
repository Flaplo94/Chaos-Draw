using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ChickenOverlordSystem : MonoBehaviour
{
    private static ChickenOverlordSystem _instance;
    private Coroutine _loop;

    [Header("Settings")]
    [SerializeField] private int hatchKillThreshold;
    [SerializeField] private bool countAllEnemyDeaths = true; // if false, use ReportKill() to credit kills
    [SerializeField] private bool includeBossDeaths = false;

    [Header("Spawn")]
    [SerializeField] private GameObject chickenAllyPrefab;
    [SerializeField] private Vector2 spawnOffset = new Vector2(0.5f, 0.2f);

    [Header("Debug")]
    [SerializeField] private bool logProgress = false;

    private int killCount = 0;
    private bool hatched = false;

    // tracking set so we can detect new/dead enemies if using countAllEnemyDeaths
    private readonly HashSet<int> aliveIds = new HashSet<int>();

    void Awake()
    {
        if (_instance == null) _instance = this;
        else if (_instance != this) { Destroy(gameObject); return; }
        DontDestroyOnLoad(gameObject);
    }

    // ---- Public API ----

    public static void Enable(GameObject chickenPrefab, int threshold = 100, bool creditAllDeaths = true)
    {
        var inst = Ensure();
        inst.chickenAllyPrefab = chickenPrefab;
        inst.hatchKillThreshold = threshold;
        inst.countAllEnemyDeaths = creditAllDeaths;

        if (inst._loop == null)
            inst._loop = inst.StartCoroutine(inst.Run());
    }

    public static void Disable()
    {
        if (_instance == null) return;
        if (_instance._loop != null)
        {
            _instance.StopCoroutine(_instance._loop);
            _instance._loop = null;
        }
    }

    // Call this from your damage code if you later add proper kill crediting
    public static void ReportKill()
    {
        var inst = Ensure();
        inst.IncrementKill();
    }

    // ---- Internals ----

    private static ChickenOverlordSystem Ensure()
    {
        if (_instance != null) return _instance;

        var found = FindFirstObjectByType<ChickenOverlordSystem>(FindObjectsInactive.Exclude);
        if (found != null) { _instance = found; return _instance; }

        var go = new GameObject("ChickenOverlordSystem");
        _instance = go.AddComponent<ChickenOverlordSystem>();
        DontDestroyOnLoad(go);
        return _instance;
    }

    private IEnumerator Run()
    {
        // seed current alive enemy ids so we only count *new* deaths
        RefreshAliveSet();

        while (!hatched)
        {
            if (countAllEnemyDeaths)
                PollForDeaths(); // count any enemy that disappears (simple & robust)

            if (killCount >= hatchKillThreshold)
            {
                Hatch();
                yield break;
            }

            yield return new WaitForSeconds(0.25f); // light polling
        }
    }

    private void RefreshAliveSet()
    {
        aliveIds.Clear();

        var enemies = FindObjectsByType<EnemyHealth>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var e in enemies)
            if (e != null && e.GetHealth() > 0)
                aliveIds.Add(e.GetInstanceID());

        if (includeBossDeaths)
        {
            var bosses = FindObjectsByType<BossHealth>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var b in bosses)
                aliveIds.Add(b.GetInstanceID());
        }
    }

    private void PollForDeaths()
    {
        var current = new HashSet<int>();

        var enemies = FindObjectsByType<EnemyHealth>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var e in enemies)
            if (e != null && e.GetHealth() > 0)
                current.Add(e.GetInstanceID());

        if (includeBossDeaths)
        {
            var bosses = FindObjectsByType<BossHealth>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var b in bosses)
                current.Add(b.GetInstanceID());
        }

        // Any id that was alive before but is now missing => counted as a death
        var toRemove = new List<int>();
        foreach (var id in aliveIds)
        {
            if (!current.Contains(id))
            {
                IncrementKill();
                toRemove.Add(id);
            }
        }
        // Update alive set: remove dead, add newly found
        foreach (var id in toRemove) aliveIds.Remove(id);
        foreach (var id in current) aliveIds.Add(id);
    }

    private void IncrementKill()
    {
        killCount++;
        if (logProgress) Debug.Log($"[ChickenEgg] Kills {killCount}/{hatchKillThreshold}");

        if (!hatched && killCount >= hatchKillThreshold)
        {
            Hatch();
        }
    }

    private void Hatch()
    {
        if (hatched) return;
        hatched = true;

        // Spawn chicken at player
        var player = FindPlayer();
        Vector3 pos = player != null ? (player.position + (Vector3)spawnOffset) : Vector3.zero;

        if (chickenAllyPrefab != null)
        {
            var ally = Instantiate(chickenAllyPrefab, pos, Quaternion.identity);
            Debug.Log("[ChickenEgg] Hatched! Chicken ally spawned.");
        }
        else
        {
            Debug.LogWarning("[ChickenEgg] Chicken prefab not assigned; cannot spawn ally.");
        }

        // Stop counting (one-time hatch)
        if (_loop != null) { StopCoroutine(_loop); _loop = null; }
    }

    private Transform FindPlayer()
    {
        var tagged = GameObject.FindGameObjectWithTag("Player");
        if (tagged) return tagged.transform;
        var pm = FindFirstObjectByType<PlayerMovement>(FindObjectsInactive.Exclude);
        return pm ? pm.transform : null;
    }
}
