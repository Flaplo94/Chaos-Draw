using UnityEngine;

public class DecklessSystem : MonoBehaviour
{
    public static bool IsActive { get; private set; }

    public static void Enable()
    {
        IsActive = true;

        // Flip CardHandUI into deckless mode right away if it's around
        var hand = FindFirstObjectByType<CardHandUI>(FindObjectsInactive.Exclude);
        if (hand != null) hand.EnableDecklessRuntime();
        Debug.Log("[Deckless] Enabled: deck cleared, waiting for post-wave grants.");
    }

    public static void Disable()
    {
        IsActive = false;
    }
}
