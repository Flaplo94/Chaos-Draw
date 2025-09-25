using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance;

    public readonly HashSet<ArtifactData> artifacts = new();
    public readonly HashSet<ItemData> items = new();

    /// <summary>Fyrer hver gang hele inventory ændrer sig.</summary>
    public event Action OnInventoryChanged;

    /// <summary>Fyrer specifikt når et artifact tilføjes.</summary>
    public event Action<ArtifactData> OnArtifactAdded;

    /// <summary>Fyrer specifikt når en buff tilføjes.</summary>
    public event Action<ItemData> OnItemAdded;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    // --- Queries ---
    public bool Has(ArtifactData a) => a != null && artifacts.Contains(a);
    public bool Has(ItemData b) => b != null && items.Contains(b);

    // --- Mutations ---
    public bool AddArtifact(ArtifactData a)
    {
        if (a == null) return false;

        bool added = artifacts.Add(a);

        OnInventoryChanged?.Invoke();
        OnArtifactAdded?.Invoke(a);

        //  NYT: Apply effekten i ArtifactSystem
        if (added && ArtifactSystem.Instance != null)
            ArtifactSystem.Instance.Apply(a);

        return added;
    }

    public bool AddItem(ItemData b)
    {
        if (b == null) return false;

        bool added = items.Add(b);

        OnInventoryChanged?.Invoke();
        OnItemAdded?.Invoke(b);

        // Apply item effects immediately (mirror artifacts)
        if (added && PlayerItemManager.Instance != null)
            PlayerItemManager.Instance.AddItem(b);

        return added;
    }

    // --- BAKKOMPAT ---
    public void Add(ArtifactData a) { AddArtifact(a); }
    public void Add(ItemData iOld) { AddItem(iOld); }
}
