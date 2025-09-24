using UnityEngine;

public class ResetTester : MonoBehaviour
{
    [ContextMenu("Reset DeckOfFate Unlock")]
    public void ResetDeckOfFateUnlock()
    {
        PlayerPrefs.SetInt("DeckOfFateUnlocked", 0);
        PlayerPrefs.Save();
        Debug.Log(" Deck of Fate & Skill Tree lock reset!");
    }

    [ContextMenu("Reset All")]
    public void ResetAll()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("All PlayerPrefs deleted!");
    }
}
