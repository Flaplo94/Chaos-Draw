using UnityEngine;

/// Service der holder prefab reference til banana-projektil
public class BananaSpawnerService : MonoBehaviour
{
    public static BananaSpawnerService Instance;
    public GameObject bananaProjectilePrefab;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
}
