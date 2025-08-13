using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance;

    public readonly HashSet<ArtifactData> artifacts = new();
    public readonly HashSet<BuffData> buffs = new();

    void Awake() { if (Instance == null) Instance = this; else Destroy(gameObject); }

    public bool Has(ArtifactData a) => a && artifacts.Contains(a);
    public bool Has(BuffData b) => b && buffs.Contains(b);

    public void Add(ArtifactData a)
    {
        if (!a || artifacts.Contains(a)) return;
        artifacts.Add(a);
        // TODO: ArtifactSystem.Instance.Apply(a);
    }

    public void Add(BuffData b)
    {
        if (!b || buffs.Contains(b)) return;
        buffs.Add(b);
        // TODO: BuffSystem.Instance.Apply(b);
    }
}
