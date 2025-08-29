using UnityEngine;

/// Artifact: Deckless. Stub flag du kan tjekke i Deck/Wave logik.
public static class DecklessSystem
{
    public static bool Enabled { get; private set; }

    public static void Enable()
    {
        Enabled = true;
        Debug.Log("[Deckless] Enabled - fjern deck og giv +2 random kort ved wave-end");
    }
}
