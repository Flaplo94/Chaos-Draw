using UnityEngine;

public class ResetTester : MonoBehaviour
{
    [ContextMenu("Reset Legacy Unlock")]
    public void ResetLegacyUnlock()
    {
        PlayerPrefs.SetInt("LegacyUnlocked", 0);
        PlayerPrefs.Save();
        Debug.Log("Legacy Deck & Skill Tree lock reset!");
    }

    [ContextMenu("Reset All")]
    public void ResetAll()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("All PlayerPrefs deleted!");
    }
}
