// Wallet.cs
using System;
using UnityEngine;

public class Wallet : MonoBehaviour
{
    public static Wallet Instance;

    [Header("Startvaerdi")]
    [SerializeField] private int startingGold = 0;

    // Intern beholdning
    [SerializeField] private int gold;

    // UI/andre systemer forventer disse:
    public int Gold { get { return gold; } }
    public event Action<int> OnGoldChanged;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        gold = Mathf.Max(0, startingGold);
        RaiseChanged();
    }

    // Tilfoej guld med BUFF/ARTIFACT multiplikator (Golden Idol m.m.)
    public int Add(int baseAmount)
    {
        int add = Mathf.Max(0, ApplyGoldMultiplier(baseAmount));
        if (add == 0) return 0;

        gold += add;
        RaiseChanged();
        Debug.Log("[Wallet] +" + add + " gold (base " + baseAmount + ")");
        return add;
    }

    // Ra tilfoejelse uden multiplikator (hvis noedvendigt i tests/tools)
    public int AddRaw(int amount)
    {
        int add = Mathf.Max(0, amount);
        if (add == 0) return 0;

        gold += add;
        RaiseChanged();
        Debug.Log("[Wallet] +RAW " + add + " gold");
        return add;
    }

    // Forsog at bruge guld. Returnerer true hvis succes.
    public bool TrySpend(int amount)
    {
        if (amount <= 0) return true;
        if (gold < amount) return false;

        gold -= amount;
        RaiseChanged();
        return true;
    }

    public bool CanAfford(int amount)
    {
        return gold >= Mathf.Max(0, amount);
    }

    // Sæt direkte (bruges sjældent, men nogle UI kan kalde det)
    public void SetGold(int value)
    {
        gold = Mathf.Max(0, value);
        RaiseChanged();
    }

    // Hjælpere
    private int ApplyGoldMultiplier(int baseAmount)
    {
        float mult = 1f;
        if (PlayerBuffManager.Instance != null)
            mult = PlayerBuffManager.Instance.GetGoldGainMult(); // inkluderer Golden Idol runtime-bonus

        float f = baseAmount * mult;
        int result = Mathf.RoundToInt(f);
        return result < 0 ? 0 : result;
    }

    private void RaiseChanged()
    {
        var handler = OnGoldChanged;
        if (handler != null) handler(gold);
    }
}
