using UnityEngine;

public class QuickSpawnTest : MonoBehaviour
{
    public GameObject prefab;   // sæt din enemy prefab her
    public Transform point;     // sæt et spawn point her

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            if (prefab == null || point == null)
            {
                Debug.LogError("[QuickSpawnTest] Mangler prefab eller point");
                return;
            }
            Instantiate(prefab, point.position, Quaternion.identity);
            Debug.Log("[QuickSpawnTest] Spawned test enemy");
        }
    }
}
