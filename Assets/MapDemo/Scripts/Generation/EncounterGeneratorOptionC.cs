using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Option C: strict budget per row.
/// Each encounter type has a cost; each row has a budget.
/// For each row 1..last-1:
/// - Generate ALL combinations of [Normal, Elite, Special, Event, Shop]
///   for that row's node count.
/// - Compute total cost for each.
/// - Keep only combos with totalCost <= budget[r].
/// - Choose a random valid combo and assign it.
/// - If no valid combos exist, leave row as all Normal and mark FAIL.
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

        // 1) Initialise encounter types: Start / Boss / Normal
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

        // 2) Apply budget per row (rows 1..last-1)
        for (int r = 1; r < lastRow; r++)
        {
            var rowNodes = graph.rows[r];
            if (rowNodes == null || rowNodes.Count == 0)
                continue;

            int budget = GetRowBudget(settings, r);
            if (budget <= 0)
            {
                // No money for this row -> keep all Normal, mark FAIL
                pass = false;
                continue;
            }

            bool rowPass = ApplyBudgetToRow(rowNodes, budget, rng, settings);
            if (!rowPass)
                pass = false;
        }
    }

    // ----------------------------------------------------------
    // Row assignment with strict budget (never > budget)
    // ----------------------------------------------------------

    private bool ApplyBudgetToRow(List<MapNodeData> rowNodes, int budget, System.Random rng, OptionCSettings s)
    {
        int n = rowNodes.Count;

        int minAllowed = budget - 1;
        int maxAllowed = budget + 1;

        EncounterType[] options =
        {
        EncounterType.Normal,
        EncounterType.Elite,
        EncounterType.Special,
        EncounterType.Event,
        EncounterType.Shop
    };

        var current = new EncounterType[n];
        var valid = new List<EncounterType[]>();

        void Search(int idx, int costSoFar)
        {
            // prune: even if we fill remaining with cheapest, can we still reach minAllowed?
            int remaining = n - idx;
            int minPossible = costSoFar + remaining * s.costNormal;
            if (minPossible > maxAllowed) return;

            if (idx == n)
            {
                if (costSoFar >= minAllowed && costSoFar <= maxAllowed)
                {
                    var combo = new EncounterType[n];
                    Array.Copy(current, combo, n);
                    valid.Add(combo);
                }
                return;
            }

            for (int i = 0; i < options.Length; i++)
            {
                var t = options[i];
                int c = GetCost(t, s);
                int next = costSoFar + c;

                if (next > maxAllowed) continue; // never go over
                current[idx] = t;
                Search(idx + 1, next);
            }
        }

        Search(0, 0);

        if (valid.Count == 0)
            return false; // no combos fit the window

        var chosen = valid[rng.Next(valid.Count)];
        for (int i = 0; i < n; i++)
            rowNodes[i].encounterType = chosen[i];

        return true;
    }

    // ----------------------------------------------------------
    // Helpers
    // ----------------------------------------------------------

    private int GetRowBudget(OptionCSettings s, int rowIndex)
    {
        if (s.rowBudgets == null || s.rowBudgets.Length == 0)
            return 0;

        if (rowIndex < 0) rowIndex = 0;
        if (rowIndex >= s.rowBudgets.Length)
            rowIndex = s.rowBudgets.Length - 1;

        return s.rowBudgets[rowIndex];
    }

    private int GetCost(EncounterType type, OptionCSettings s)
    {
        switch (type)
        {
            case EncounterType.Elite: return s.costElite;
            case EncounterType.Special: return s.costSpecial;
            case EncounterType.Event: return s.costEvent;
            case EncounterType.Shop: return s.costShop;

            case EncounterType.Start:
            case EncounterType.Boss:
            case EncounterType.Normal:
            default:
                return s.costNormal;
        }
    }
}
