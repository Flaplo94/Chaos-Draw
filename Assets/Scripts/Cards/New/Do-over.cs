using UnityEngine;

public class DoOver : MonoBehaviour
{
    private CardHandUI hand;

    void Start()
    {
        hand = FindFirstObjectByType<CardHandUI>();
        if (hand == null)
        {
            Debug.LogWarning("[DoOver] No CardHandUI found in scene!");
            Destroy(gameObject);
            return;
        }

        // Discard everything, then draw a new hand
        //hand.DiscardHand();
        //hand.DrawToHandSize();

        Destroy(gameObject); // instantly done
    }
}
