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
        int baseCost = s.costNormal * count;

        // If even all-Normal exceeds budget, can't satisfy this row.
        if (baseCost > budget)
        {
            globalPass = false;
            // Keep them all Normal, but we violated the budget.
            return;
        }

        // Start with all Normal (already set in Generate())
        int remainingBudget = budget - baseCost;

        if (remainingBudget <= 0)
            return; // exactly at budget with all Normal

        // We will walk nodes in random order and try upgrading Normal  {Elite, Special, Event, Shop}
        var nodeIndices = new List<int>(count);
        for (int i = 0; i < count; i++)
            nodeIndices.Add(i);

        Shuffle(nodeIndices, rng);

        foreach (int idx in nodeIndices)
        {
            var node = rowNodes[idx];

            if (node.encounterType != EncounterType.Normal)
                continue; // should be Normal, but just in case

            // Try all possible upgrades whose incremental cost fits remainingBudget
            var possibleTypes = GetPossibleUpgrades(remainingBudget, s);

            if (possibleTypes.Count == 0)
                break; // can't upgrade anything more within budget

            // Pick one at random
            var newType = possibleTypes[rng.Next(possibleTypes.Count)];

            int incCost = GetCost(newType, s) - s.costNormal;
            if (incCost <= remainingBudget && incCost > 0)
            {
                node.encounterType = newType;
                remainingBudget -= incCost;
            }

            if (remainingBudget <= 0)
                break;
        }

        // By construction we never exceed the budget for this row.
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
}
