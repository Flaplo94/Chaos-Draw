using System;
using UnityEngine;

public class Wallet : MonoBehaviour
{
    public static Wallet Instance;

    [Header("Startværdi")]
    [SerializeField] private int startingGold = 0;

    private int gold;

    // Properties til UI
    public int CurrentGold => gold; //  til TopBarUI
    public event Action<int> OnGoldChanged;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        gold = Mathf.Max(0, startingGold);
        RaiseChanged();

        Debug.Log($"[Wallet] Awake -> StartingGold={startingGold}, CurrentGold={gold}");
    }

    public int Add(int baseAmount)
    {
        int add = Mathf.Max(0, ApplyGoldMultiplier(baseAmount));
        if (add == 0) return 0;

        gold += add;
        RaiseChanged();
        Debug.Log("[Wallet] +" + add + " gold (base " + baseAmount + ")");
        return add;
    }

    public int AddRaw(int amount)
    {
        int add = Mathf.Max(0, amount);
        if (add == 0) return 0;

        gold += add;
        RaiseChanged();
        Debug.Log("[Wallet] +RAW " + add + " gold");
        return add;
    }

    public bool TrySpend(int amount)
    {
        if (amount <= 0) return true;
        if (gold < amount) return false;

        gold -= amount;
        RaiseChanged();
        return true;
    }

    public bool CanAfford(int amount) => gold >= Mathf.Max(0, amount);

    public void SetGold(int value)
    {
        gold = Mathf.Max(0, value);
        RaiseChanged();
    }

    private int ApplyGoldMultiplier(int baseAmount)
    {
        float mult = 1f;
        if (PlayerBuffManager.Instance != null)
            mult = PlayerBuffManager.Instance.GetGoldGainMult();

        float f = baseAmount * mult;
        return Mathf.RoundToInt(f);
    }

    private void RaiseChanged() => OnGoldChanged?.Invoke(gold);
}
