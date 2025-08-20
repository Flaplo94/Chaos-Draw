using UnityEngine;

/// Service der holder prefab reference til en orbiter (Holy Cheese)
public class OrbiterSystemService : MonoBehaviour
{
    public static OrbiterSystemService Instance;
    public GameObject cheeseOrbiterPrefab;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
}
