using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MapController : MonoBehaviour
{
    [Header("Chunk Settings")]
    public int chunkSize = 20;
    public int loadRadiusChunks = 4;
    public int unloadRadiusChunks = 5;

    [Header("Prefabs")]
    public GameObject[] chunkPrefabs;   // Terræn-chunks

    [Header("Runtime")]
    public Transform player;            // (drag Player her i Inspector)
    [HideInInspector] public GameObject currentChunk;

    private readonly Dictionary<Vector2Int, GameObject> loadedChunks = new();
    private Vector2Int _lastCenter;

    private void Start()
    {
        // Preload omkring spilleren ved scene start
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }
        if (player) UpdateLoadedChunks(player.position);
    }

    private void Update()
    {
        // Fallback: hvis vi ikke har fået OnChunkChanged endnu, så tjek selv
        if (!player) return;

        var centerNow = WorldToChunk(player.position);
        if (centerNow != _lastCenter || currentChunk == null)
        {
            UpdateLoadedChunks(player.position);
            _lastCenter = centerNow;
        }
    }

    // Kaldt af ChunkTrigger når spilleren går ind i en chunk
    public void OnChunkChanged(Vector3 playerPos)
    {
        UpdateLoadedChunks(playerPos);
        _lastCenter = WorldToChunk(playerPos);
    }

    private Vector2Int WorldToChunk(Vector3 worldPos)
    {
        int cx = Mathf.FloorToInt(worldPos.x / chunkSize);
        int cy = Mathf.FloorToInt(worldPos.y / chunkSize);
        return new Vector2Int(cx, cy);
    }

    private void UpdateLoadedChunks(Vector3 playerPos)
    {
        var center = WorldToChunk(playerPos);

        // Load indenfor R
        for (int y = -loadRadiusChunks; y <= loadRadiusChunks; y++)
            for (int x = -loadRadiusChunks; x <= loadRadiusChunks; x++)
            {
                var c = new Vector2Int(center.x + x, center.y + y);
                EnsureChunkLoaded(c);
            }

        // Unload udenfor R+1
        foreach (var kv in loadedChunks.ToList())
        {
            var c = kv.Key;
            if (Mathf.Abs(c.x - center.x) > unloadRadiusChunks ||
                Mathf.Abs(c.y - center.y) > unloadRadiusChunks)
            {
                UnloadChunk(c);
            }
        }
    }

    private void EnsureChunkLoaded(Vector2Int c)
    {
        if (loadedChunks.ContainsKey(c)) return;

        var prefab = chunkPrefabs[Random.Range(0, chunkPrefabs.Length)];
        var pos = new Vector3(c.x * chunkSize, c.y * chunkSize, 0);

        var chunk = Instantiate(prefab, pos, Quaternion.identity, transform);
        loadedChunks[c] = chunk;
    }

    private void UnloadChunk(Vector2Int c)
    {
        if (!loadedChunks.TryGetValue(c, out var go)) return;
        Destroy(go);
        loadedChunks.Remove(c);
    }

#if UNITY_EDITOR
    // Hjælp til visuel verifikation i editor
    private void OnDrawGizmosSelected()
    {
        if (!player) return;
        var center = WorldToChunk(player.position);
        Gizmos.matrix = Matrix4x4.identity;
        for (int y = -loadRadiusChunks; y <= loadRadiusChunks; y++)
            for (int x = -loadRadiusChunks; x <= loadRadiusChunks; x++)
            {
                var wp = new Vector3((center.x + x) * chunkSize, (center.y + y) * chunkSize, 0);
                Gizmos.DrawWireCube(wp + new Vector3(chunkSize / 2f, chunkSize / 2f, 0), new Vector3(chunkSize, chunkSize, 0.1f));
            }
    }
#endif
}
