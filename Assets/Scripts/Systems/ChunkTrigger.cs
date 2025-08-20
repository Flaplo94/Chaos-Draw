using UnityEngine;

public class ChunkTrigger : MonoBehaviour
{
    private MapController mc;
    public GameObject targetMap;   // Peger på chunk-roden

    private void Awake()
    {
        mc = FindFirstObjectByType<MapController>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // Sæt currentChunk og kald MapController til at opdatere radius
        if (mc.currentChunk != targetMap)
        {
            mc.currentChunk = targetMap;
            mc.OnChunkChanged(other.transform.position);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (mc.currentChunk == targetMap)
        {
            mc.currentChunk = null;
        }
    }
}
