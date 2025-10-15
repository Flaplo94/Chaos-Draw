using System.Collections;
using UnityEngine;

public class DoOver : MonoBehaviour, IAbilityBehavior
{
    public bool Initialize(Vector2 _, Rarity __)
    {
        // Wait a frame so TryUseCard() can move the played card to discard + clear its slot.
        StartCoroutine(ExecuteNextFrame());
        return true; // treat cast as consumed
    }

    private IEnumerator ExecuteNextFrame()
    {
        yield return null; // next frame: the used slot is now null

        var hand = FindFirstObjectByType<CardHandUI>();
        if (hand == null) { Destroy(gameObject); yield break; }

        hand.DiscardHandOnly();
        hand.DrawFullHand();

        Destroy(gameObject);
    }
}


