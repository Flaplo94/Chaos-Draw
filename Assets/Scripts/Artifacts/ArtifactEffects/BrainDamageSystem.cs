using System.Collections.Generic;
using UnityEngine;

public class BrainDamageSystem : MonoBehaviour
{
    static BrainDamageSystem _instance;

    // Store originals as FLOATS to match Ability.manaCost
    static readonly Dictionary<Ability, float> originalCosts = new();

    void Awake()
    {
        if (_instance == null) _instance = this;
        else if (_instance != this) { Destroy(gameObject); return; }
        DontDestroyOnLoad(gameObject);
    }

    static BrainDamageSystem Ensure()
    {
        if (_instance != null) return _instance;
        var go = new GameObject("BrainDamageSystem");
        _instance = go.AddComponent<BrainDamageSystem>();
        DontDestroyOnLoad(go);
        return _instance;
    }

    public static void Enable()
    {
        Ensure();

        // Shuffle 1/2/3/4  slot mapping
        InputShuffleSystem.ShuffleKeys();

        // Force every card’s mana cost to 1f
        var all = Resources.LoadAll<Ability>(""); // load all Ability assets
        foreach (var a in all)
        {
            if (!a) continue;
            if (!originalCosts.ContainsKey(a)) originalCosts[a] = a.manaCost; // float  float
            a.manaCost = 1f; // use float literal
        }

        // Optional: nudge UI to refresh (safe if method not present)
        var handUI = Object.FindFirstObjectByType<CardHandUI>(FindObjectsInactive.Exclude);
        if (handUI != null)
            handUI.SendMessage("UpdateCardOverlays", SendMessageOptions.DontRequireReceiver);

        Debug.Log("[BrainDamage] All abilities set to 1 mana and inputs shuffled.");
    }

    public static void DisableAndRestore()
    {
        foreach (var kv in originalCosts)
            if (kv.Key) kv.Key.manaCost = kv.Value; // restore float cost
        originalCosts.Clear();
        Debug.Log("[BrainDamage] Restored original mana costs.");
    }
}
