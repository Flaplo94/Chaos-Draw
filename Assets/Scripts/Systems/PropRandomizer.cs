using System.Collections.Generic;
using UnityEngine;

public class PropRandomizer : MonoBehaviour
{
    [Header("Inputs")]
    public List<GameObject> propPrefabs;    // dine Objects_* prefabs

    [Header("Auto-detect")]
    [Tooltip("Navn på containeren der indeholder spawn points.")]
    public string spawnParentName = "Props";
    [Tooltip("Kun children hvis navn indeholder dette (tom = alle).")]
    public string spawnPointNameContains = "Prop Location";

    [Header("Anti-overlap (størrelsesbevidst)")]
    [Tooltip("Fallback-radius hvis vi ikke kan udlede størrelse fra sprite.")]
    public float defaultRadius = 0.6f;
    [Tooltip("Ekstra afstand lagt oven i to radiusser.")]
    public float minSeparationPadding = 0.1f;
    [Tooltip("Tilfældig offset omkring punktet.")]
    public float jitterRadius = 0.4f;
    [Tooltip("Maks. forsøg pr. spawn point før vi skipper.")]
    public int maxAttemptsPerPoint = 12;
    [Tooltip("Undgå overlap på tværs af alle chunks.")]
    public bool avoidAcrossChunks = true;

    [Header("Debug")]
    public bool drawGizmos = false;

    // ----- intern data -----
    private readonly List<Transform> _spawnPoints = new();
    private readonly List<Circle> _myCircles = new(); // hvad denne chunk har placeret

    // Global liste (alle PropRandomizer-instancer i scenen)
    private static readonly List<Circle> _globalOccupied = new();

    private struct Circle
    {
        public Vector2 pos;
        public float r;
        public Circle(Vector2 p, float radius) { pos = p; r = radius; }
    }

    private void Awake()
    {
        Transform parent = transform.Find(spawnParentName);
        if (!parent)
        {
            Debug.LogWarning($"[PropRandomizer] Fandt ikke '{spawnParentName}' under {name}. Ingen props spawnes.");
            return;
        }

        foreach (Transform child in parent)
        {
            if (string.IsNullOrEmpty(spawnPointNameContains) || child.name.Contains(spawnPointNameContains))
                _spawnPoints.Add(child);
        }

        if (_spawnPoints.Count == 0)
            Debug.LogWarning($"[PropRandomizer] Ingen spawn points under '{spawnParentName}' på {name}.");
    }

    private void Start()
    {
        SpawnProps();
    }

    private void OnDestroy()
    {
        // Fjern mine cirkler fra global listen når chunk unloades
        if (_myCircles.Count == 0) return;
        for (int i = _globalOccupied.Count - 1; i >= 0; i--)
        {
            for (int j = 0; j < _myCircles.Count; j++)
            {
                if (Approximately(_globalOccupied[i], _myCircles[j]))
                {
                    _globalOccupied.RemoveAt(i);
                    break;
                }
            }
        }
        _myCircles.Clear();
    }

    private void SpawnProps()
    {
        if (_spawnPoints.Count == 0 || propPrefabs == null || propPrefabs.Count == 0) return;

        Shuffle(_spawnPoints);

        foreach (var sp in _spawnPoints)
        {
            int attempts = 0;
            bool placed = false;

            while (attempts++ < maxAttemptsPerPoint && !placed)
            {
                var prefab = propPrefabs[Random.Range(0, propPrefabs.Count)];
                float r = GetPrefabRadius(prefab);

                Vector2 basePos = sp.position;
                Vector2 candidate = basePos + (jitterRadius > 0f ? Random.insideUnitCircle * jitterRadius : Vector2.zero);

                if (IsFarEnough(candidate, r))
                {
                    var go = Instantiate(prefab, candidate, Quaternion.identity);
                    go.transform.SetParent(sp, worldPositionStays: true);

                    var circle = new Circle(candidate, r);
                    _myCircles.Add(circle);
                    if (avoidAcrossChunks) _globalOccupied.Add(circle);

                    placed = true;
                }
            }
            // Hvis vi ikke fandt plads, skipper vi punktet
        }
    }

    private bool IsFarEnough(Vector2 p, float r)
    {
        float pad = Mathf.Max(0f, minSeparationPadding);

        if (avoidAcrossChunks)
        {
            for (int i = 0; i < _globalOccupied.Count; i++)
            {
                float rr = r + _globalOccupied[i].r + pad;
                if ((p - _globalOccupied[i].pos).sqrMagnitude < rr * rr)
                    return false;
            }
        }
        // Tjek også imod lokale (skulle være overflødigt hvis avoidAcrossChunks=true,
        // men gør det robust hvis man slår det fra)
        for (int i = 0; i < _myCircles.Count; i++)
        {
            float rr = r + _myCircles[i].r + pad;
            if ((p - _myCircles[i].pos).sqrMagnitude < rr * rr)
                return false;
        }
        return true;
    }

    private float GetPrefabRadius(GameObject prefab)
    {
        // 1) Prøv en valgfri "PropFootprint" hvis den findes (uden compile-time afhængighed)
        var t = System.Type.GetType("PropFootprint");
        if (t != null)
        {
            var comp = prefab.GetComponent(t);
            if (comp != null)
            {
                var field = t.GetField("radius");
                if (field != null && field.FieldType == typeof(float))
                {
                    float r = (float)field.GetValue(comp);
                    if (r > 0.01f) return r;
                }
            }
        }

        // 2) Udled fra SpriteRenderer (bounds * scale)
        var sr = prefab.GetComponentInChildren<SpriteRenderer>(true);
        if (sr && sr.sprite)
        {
            var size = sr.sprite.bounds.size; // i Unity units
            var tr = sr.transform;
            float w = Mathf.Abs(size.x * tr.localScale.x);
            float h = Mathf.Abs(size.y * tr.localScale.y);
            return 0.5f * Mathf.Max(w, h);
        }

        // 3) Fallback
        return defaultRadius;
    }


    private static void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private static bool Approximately(Circle a, Circle b)
    {
        return Mathf.Approximately(a.r, b.r) && (a.pos - b.pos).sqrMagnitude < 0.0001f;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;
        Gizmos.matrix = Matrix4x4.identity;

        // tegn mine cirkler
        Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.25f);
        foreach (var c in _myCircles) DrawWireDisc(c.pos, c.r);

        // og globale (svagt røde), så du kan se grænser på tværs af chunks
        if (avoidAcrossChunks)
        {
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.15f);
            foreach (var c in _globalOccupied) DrawWireDisc(c.pos, c.r);
        }
    }

    private static void DrawWireDisc(Vector2 center, float r)
    {
        const int steps = 32;
        Vector3 prev = center + new Vector2(r, 0);
        for (int i = 1; i <= steps; i++)
        {
            float ang = (i / (float)steps) * Mathf.PI * 2f;
            Vector3 next = center + new Vector2(Mathf.Cos(ang) * r, Mathf.Sin(ang) * r);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
#endif
}
