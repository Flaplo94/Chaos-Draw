using System.Collections.Generic;
using UnityEngine;

public class PropRandomizer : MonoBehaviour
{
    [Header("Inputs")]
    public List<GameObject> propPrefabs;   // Dine Objects_* prefabs

    [Header("Auto-detect")]
    [Tooltip("Navn på containeren der indeholder spawn points.")]
    public string spawnParentName = "Props";

    [Tooltip("Kun children hvis navn indeholder dette (tom = alle).")]
    public string spawnPointNameContains = "Prop Location";

    [Header("Anti-overlap")]
    public float minSeparation = 1.25f;   // min afstand center-til-center
    public float jitterRadius = 0.4f;     // lille tilfældig offset
    public int maxAttemptsPerPoint = 10;  // forsøg pr. punkt før vi skipper

    private readonly List<Vector2> occupied = new();
    private readonly List<Transform> spawnPoints = new();

    private void Awake()
    {
        // Find alle spawn points under "Props"
        Transform parent = transform.Find(spawnParentName);
        if (parent == null)
        {
            Debug.LogWarning($"[PropRandomizer] Kunne ikke finde '{spawnParentName}' under {name}. Ingen props spawnes.");
            return;
        }

        foreach (Transform child in parent)
        {
            if (string.IsNullOrEmpty(spawnPointNameContains) || child.name.Contains(spawnPointNameContains))
                spawnPoints.Add(child);
        }

        if (spawnPoints.Count == 0)
            Debug.LogWarning($"[PropRandomizer] Fandt ingen spawn points under '{spawnParentName}' på {name}.");
    }

    private void Start()
    {
        SpawnProps();
    }

    private void SpawnProps()
    {
        if (spawnPoints.Count == 0 || propPrefabs == null || propPrefabs.Count == 0) return;

        // Shuffl’e punkterne så placering varierer
        Shuffle(spawnPoints);

        foreach (var sp in spawnPoints)
        {
            int attempts = 0;
            bool placed = false;

            while (attempts++ < maxAttemptsPerPoint && !placed)
            {
                var prefab = propPrefabs[Random.Range(0, propPrefabs.Count)];
                Vector2 basePos = sp.position;
                Vector2 candidate = basePos + (jitterRadius > 0f ? Random.insideUnitCircle * jitterRadius : Vector2.zero);

                if (IsFarEnough(candidate))
                {
                    var go = Instantiate(prefab, candidate, Quaternion.identity);
                    go.transform.SetParent(sp, worldPositionStays: true); // som før
                    occupied.Add(candidate);
                    placed = true;
                }
            }
            // hvis ingen plads  vi skipper bare punktet
        }
    }

    private bool IsFarEnough(Vector2 p)
    {
        float minSqr = minSeparation * minSeparation;
        for (int i = 0; i < occupied.Count; i++)
            if ((occupied[i] - p).sqrMagnitude < minSqr) return false;
        return true;
    }

    private static void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
