using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Option C: encounter placement controlled by a budget per row.
/// Each encounter type has a cost; each row has a budget.
/// Rows 0 and last are Start/Boss; rows 1..last-1 are budgeted.
/// </summary>
public class EncounterGeneratorOptionC
{
    public void Generate(MapGraph graph, System.Random rng, OptionCSettings settings, out bool pass)
    {
        pass = true;

        if (graph == null || graph.rows == null || graph.rows.Count == 0 || settings == null)
        {
            pass = false;
            return;
        }

        int lastRow = graph.totalRows - 1;

        // 1) Initialize encounter types: Start / Boss / Normal
        for (int r = 0; r < graph.rows.Count; r++)
        {
            foreach (var node in graph.rows[r])
            {
                if (r == 0)
                    node.encounterType = EncounterType.Start;
                else if (r == lastRow)
                    node.encounterType = EncounterType.Boss;
                else
                    node.encounterType = EncounterType.Normal;
            }
        }

        // 2) Apply per-row budgets to rows 1..last-1
        for (int r = 1; r < lastRow; r++)
        {
            var rowNodes = graph.rows[r];
            if (rowNodes == null || rowNodes.Count == 0)
                continue;

            int budget = GetRowBudget(settings, r);
            if (budget <= 0)
            {
                // No budget -> all Normal, mark as fail but continue
                pass = false;
                continue;
            }

            ApplyBudgetToRow(rowNodes, budget, rng, settings, ref pass);
        }
    }

    // --------------------------------------------------------------------
    // Per-row budget logic
    // --------------------------------------------------------------------

    private void ApplyBudgetToRow(List<MapNodeData> rowNodes, int budget, System.Random rng, OptionCSettings s, ref bool globalPass)
    {
        int count = rowNodes.Count;

        // 1) Start with all Normal already, compute base cost
        int baseCost = count * s.costNormal;

        // 2) The allowed range is: [budget-1, budget+1]
        int minAllowed = budget - 1;
        int maxAllowed = budget + 1;

        // If even all-Normal is above maxAllowed  automatic FAIL
        if (baseCost > maxAllowed)
        {
            globalPass = false;
            return;   // can't fix this row
        }

        // If all-Normal is already inside allowed window  nothing to do
        if (baseCost >= minAllowed && baseCost <= maxAllowed)
            return;

        int remaining = maxAllowed - baseCost;  // how many points we can add safely

        // 3) Shuffle nodes and attempt upgrades SAFELY
        var indices = new List<int>(count);
        for (int i = 0; i < count; i++) indices.Add(i);
        Shuffle(indices, rng);

        foreach (int idx in indices)
        {
            var node = rowNodes[idx];
            if (node.encounterType != EncounterType.Normal)
                continue;

            // All upgrade options with incremental cost <= remaining window
            var up = GetSafeUpgrades(remaining, s);

            if (up.Count == 0)
                break;

            EncounterType chosen = up[rng.Next(up.Count)];

            int inc = GetCost(chosen, s) - s.costNormal;
            if (inc <= remaining)
            {
                node.encounterType = chosen;
                remaining -= inc;

                // Recompute row cost (safe)
                int rowCost = ComputeRowCost(rowNodes, s);

                if (rowCost > maxAllowed)
                {
                    // Undo (too expensive)
                    node.encounterType = EncounterType.Normal;
                    remaining += inc;
                }
            }
        }

        // 4) Final score check
        int finalCost = ComputeRowCost(rowNodes, s);

        if (!(finalCost >= minAllowed && finalCost <= maxAllowed))
            globalPass = false;
    }

    private List<EncounterType> GetPossibleUpgrades(int remainingBudget, OptionCSettings s)
    {
        var result = new List<EncounterType>();

        TryAddUpgrade(EncounterType.Elite, remainingBudget, s, result);
        TryAddUpgrade(EncounterType.Special, remainingBudget, s, result);
        TryAddUpgrade(EncounterType.Event, remainingBudget, s, result);
        TryAddUpgrade(EncounterType.Shop, remainingBudget, s, result);

        return result;
    }

    private void TryAddUpgrade(EncounterType type, int remainingBudget, OptionCSettings s, List<EncounterType> list)
    {
        int inc = GetCost(type, s) - s.costNormal;
        if (inc > 0 && inc <= remainingBudget)
            list.Add(type);
    }

    private int GetCost(EncounterType type, OptionCSettings s)
    {
        switch (type)
        {
            case EncounterType.Elite: return s.costElite;
            case EncounterType.Special: return s.costSpecial;
            case EncounterType.Event: return s.costEvent;
            case EncounterType.Shop: return s.costShop;
            case EncounterType.Normal:
            case EncounterType.Start:
            case EncounterType.Boss:
            default:
                return s.costNormal;
        }
    }

    private int GetRowBudget(OptionCSettings s, int rowIndex)
    {
        if (s.rowBudgets == null || s.rowBudgets.Length == 0)
            return 0;

        if (rowIndex < 0)
            rowIndex = 0;
        if (rowIndex >= s.rowBudgets.Length)
            rowIndex = s.rowBudgets.Length - 1;

        return s.rowBudgets[rowIndex];
    }

    // Fisher–Yates shuffle
    private void Shuffle(List<int> list, System.Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
    private int ComputeRowCost(List<MapNodeData> row, OptionCSettings s)
    {
        int total = 0;
        foreach (var node in row)
            total += GetCost(node.encounterType, s);
        return total;
    }

    private List<EncounterType> GetSafeUpgrades(int remaining, OptionCSettings s)
    {
        var list = new List<EncounterType>();

        TryAddUpgrade(EncounterType.Elite, remaining, s, list);
        TryAddUpgrade(EncounterType.Special, remaining, s, list);
        TryAddUpgrade(EncounterType.Event, remaining, s, list);
        TryAddUpgrade(EncounterType.Shop, remaining, s, list);

        return list;
    }
}
