using UnityEngine;
using System;

[DefaultExecutionOrder(-9000)]   // kør tidligt så andre kan bruge den i Start/Awake
public class Wallet : MonoBehaviour
{
    public static Wallet Instance { get; private set; }

    [SerializeField] private int gold = 0;
    public int Gold => gold;

    public event Action<int> OnGoldChanged;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        // ingen DontDestroyOnLoad her
        // Debug.Log("[Wallet] Awake, gold=" + gold);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null; // undgå “stale” static ref
    }

    public void Add(int amount)
    {
        gold += Mathf.Max(0, amount);
        OnGoldChanged?.Invoke(gold);
    }

    public bool TrySpend(int amount)
    {
        if (gold < amount) return false;
        gold -= amount;
        OnGoldChanged?.Invoke(gold);
        return true;
    }
}
