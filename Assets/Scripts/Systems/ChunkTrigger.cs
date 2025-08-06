using UnityEngine;

public class ChunkTrigger : MonoBehaviour
{
    MapController mc;
    public GameObject targetMap;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mc = FindFirstObjectByType<MapController>();
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            mc.currentChunk = targetMap;
        }
    }
    private void OnTriggerExit2D(Collider2D col)
    {
        if (col.CompareTag("Player"))
        {
            if(!mc.currentChunk == targetMap)
            {
                mc.currentChunk = null;
            }
        }  
    }
}
