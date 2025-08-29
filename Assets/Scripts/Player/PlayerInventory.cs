using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance;

    public readonly HashSet<ArtifactData> artifacts = new();
    public readonly HashSet<BuffData> buffs = new();

    /// <summary>Fyrer hver gang hele inventory ændrer sig.</summary>
    public event Action OnInventoryChanged;

    /// <summary>Fyrer specifikt når et artifact tilføjes.</summary>
    public event Action<ArtifactData> OnArtifactAdded;

    /// <summary>Fyrer specifikt når en buff tilføjes.</summary>
    public event Action<BuffData> OnBuffAdded;

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
    public bool Has(BuffData b) => b != null && buffs.Contains(b);

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

    public bool AddBuff(BuffData b)
    {
        if (b == null) return false;

        bool added = buffs.Add(b);

        OnInventoryChanged?.Invoke();
        OnBuffAdded?.Invoke(b);

        // Hvis buffs også skal have effekter direkte, kan du evt. kalde PlayerBuffManager her

        return added;
    }

    // --- BAKKOMPAT ---
    public void Add(ArtifactData a) { AddArtifact(a); }
    public void Add(BuffData bOld) { AddBuff(bOld); }
}
