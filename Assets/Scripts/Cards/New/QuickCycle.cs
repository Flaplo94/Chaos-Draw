using UnityEngine;
using System.Collections;

public class QuickCycle : MonoBehaviour, IAbilityBehavior
{
    public bool Initialize(Vector2 _, Rarity __)
    {
        StartCoroutine(Execute());
        return true; // consume the cast
    }

    private IEnumerator Execute()
    {
        // Wait one frame so the played QuickCycle is already discarded/cleared by the hand system
        yield return null;

        var hand = Object.FindFirstObjectByType<CardHandUI>();
        if (hand != null)
        {
            hand.DiscardOneRandomCard(); // discards one remaining card if any
            hand.DrawExactly(2);         // draws up to 2 new cards (never fills to 4)
        }

        Destroy(gameObject);
    }
}
