using UnityEngine;

public static class DeckUnlocks
{
    const string KeyPrefix = "DeckUnlocked_";

    public static bool IsUnlocked(DeckDefinition d)
        => d && (d.unlockedByDefault || PlayerPrefs.GetInt(KeyPrefix + d.id, 0) == 1);

    public static void Unlock(DeckDefinition d)
    {
        if (!d) return;
        PlayerPrefs.SetInt(KeyPrefix + d.id, 1);
        PlayerPrefs.Save();
    }
}
