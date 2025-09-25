#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class ClearPrefs
{
    [MenuItem("Tools/Debug/Clear PlayerPrefs")]
    public static void ClearAll()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("[Tools] PlayerPrefs cleared.");
    }

    [MenuItem("Tools/Debug/Lock AOE Pulse + Godspeed")]
    public static void LockGated()
    {
        CardUnlocks.LockAbilityByName("AOE Pulse");
        CardUnlocks.LockAbilityByName("Godspeed");
        Debug.Log("[Tools] Locked AOE Pulse + Godspeed.");
    }
}
#endif
