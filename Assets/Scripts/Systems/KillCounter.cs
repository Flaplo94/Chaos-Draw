using UnityEngine;

/// Global kill-counter. Kald RegisterKill() naar en enemy doer.
public class KillCounter : MonoBehaviour
{
    public static KillCounter Instance;
    public static int TotalKills;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public static void RegisterKill()
    {
        TotalKills++;
    }
}
