using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    public static DeckManager Instance;

    // Nu kan ALLE ScriptableObjects bruges som “kort” (Abilities, Spells, osv.)
    [SerializeField] private List<ScriptableObject> deck = new();
    public IReadOnlyList<ScriptableObject> Cards => deck;

    public event Action<ScriptableObject> OnCardAdded;
    public event Action<ScriptableObject> OnCardRemoved;

    void Awake()
    {
        if (Instance == null) Instance = this; else Destroy(gameObject);
    }

    // Tilføj et “kort” (uanset SO-type)
    public void AddCard(ScriptableObject so)
    {
        if (so == null) return;
        deck.Add(so);
        OnCardAdded?.Invoke(so);
        // Debug.Log($"[Deck] Added {so.name}");
    }

    // Bruges af RemoveCardModalUI til at vise muligheder
    public List<ScriptableObject> GetRemovableCards() => deck.ToList();

    // Fjern valgt “kort”
    public void RemoveCard(ScriptableObject so)
    {
        if (so == null) return;
        if (deck.Remove(so))
        {
            OnCardRemoved?.Invoke(so);
            // Debug.Log($"[Deck] Removed {so.name}");
        }
    }
}
