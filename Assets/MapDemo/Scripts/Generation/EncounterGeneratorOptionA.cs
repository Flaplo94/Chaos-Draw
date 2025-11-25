using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Option A: rule-based encounter placement + small repair phase,
/// operating on an already-built MapGraph.
/// 
/// This is a plain C# helper class, NOT a ScriptableObject.
/// </summary>
public class EncounterGeneratorOptionA
{
    // Target counts per map (from your spec)
    private const int ElitesMin = 7;
    private const int ElitesMax = 9;

    private const int EventsMin = 10;
    private const int EventsMax = 14;

    private const int ShopsMin = 5;
    private const int ShopsMax = 7;

    private const int SpecialsMin = 4;
    private const int SpecialsMax = 6;

    // ±1 tolerance on totals
    private const int Tolerance = 1;

    // Max repair iterations
    private const int MaxRepairIterations = 200;

    /// <summary>
    /// Main entry: applies Option A rules + repair-lite on the given graph.
    /// Only touches encounterType on MapNodeData (side nodes, not row 0 or boss row).
    /// </summary>
    /// <param name="graph">Map graph with rows/nodes already generated.</param>
    /// <param name="rng">System.Random instance for reproducible randomness.</param>
    /// <param name="pass">
    /// true if final totals are within targets (±1) and there are no 3+ shop rows in a row.
    /// </param>
    public void Generate(MapGraph graph, System.Random rng, out bool pass)
    {
        pass = false;

        if (graph == null || graph.rows == null || graph.rows.Count == 0)
            return;

        int lastRow = graph.totalRows - 1;

        // 1) Initialize encounter types: Start / Boss / None
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

        var sideNodes = GetSideNodes(graph);
        if (sideNodes.Count == 0)
        {
            // No side nodes to place encounters on  fail
            return;
        }

        // 2) Apply Option A base rules
        ApplyOptionARules(graph, sideNodes, rng);

        // 3) Repair-lite phase: adjust only side nodes until within tolerances
        ApplyRepairLite(graph, sideNodes, rng);

        // 4) Evaluate PASS/FAIL for A
        CountEncounters(graph,
                        out int eliteCount,
                        out int eventCount,
                        out int shopCount,
                        out int specialCount);

        bool countsOk =
            IsWithinWithTolerance(eliteCount, ElitesMin, ElitesMax) &&
            IsWithinWithTolerance(eventCount, EventsMin, EventsMax) &&
            IsWithinWithTolerance(shopCount, ShopsMin, ShopsMax) &&
            IsWithinWithTolerance(specialCount, SpecialsMin, SpecialsMax);

        bool shopChainOk = !HasShopRowChainOf3OrMore(graph);

        pass = countsOk && shopChainOk;
    }

    // --------------------------------------------------------------------
    // Option A: RULES
    // --------------------------------------------------------------------

    private void ApplyOptionARules(MapGraph graph, List<MapNodeData> sideNodes, System.Random rng)
    {
        if (sideNodes.Count == 0)
            return;

        // Random target counts within intervals
        int targetElites = UnityEngine.Random.Range(ElitesMin, ElitesMax + 1);
        int targetEvents = UnityEngine.Random.Range(EventsMin, EventsMax + 1);
        int targetShops = UnityEngine.Random.Range(ShopsMin, ShopsMax + 1);
        int targetSpecials = UnityEngine.Random.Range(SpecialsMin, SpecialsMax + 1);

        // Clamp totals if they exceed number of side nodes
        int totalRequested = targetElites + targetEvents + targetShops + targetSpecials;
        if (totalRequested > sideNodes.Count)
        {
            int overflow = totalRequested - sideNodes.Count;

            int reduceEvents = Mathf.Min(overflow, targetEvents);
            targetEvents -= reduceEvents;
            overflow -= reduceEvents;

            if (overflow > 0)
            {
                int reduceSpecials = Mathf.Min(overflow, targetSpecials);
                targetSpecials -= reduceSpecials;
                overflow -= reduceSpecials;
            }

            if (overflow > 0)
            {
                int reduceElites = Mathf.Min(overflow, targetElites);
                targetElites -= reduceElites;
                overflow -= reduceElites;
            }

            if (overflow > 0)
            {
                int reduceShops = Mathf.Min(overflow, targetShops);
                targetShops -= reduceShops;
                overflow -= reduceShops;
            }
        }

        // Start with all side nodes as Event by default
        foreach (var node in sideNodes)
            node.encounterType = EncounterType.Normal;

        // Assign Shops first (we'll repair chains later)
        AssignRandomType(sideNodes, EncounterType.Shop, targetShops, rng);

        // Assign Elites
        AssignRandomType(sideNodes, EncounterType.Elite, targetElites, rng);

        // Assign Specials
        AssignRandomType(sideNodes, EncounterType.Special, targetSpecials, rng);

        // Assign Events too
        AssignRandomType(sideNodes, EncounterType.Event, targetEvents, rng);

    }

    private void AssignRandomType(List<MapNodeData> sideNodes, EncounterType type, int targetCount, System.Random rng)
    {
        if (targetCount <= 0) return;

        // choose among nodes that are still Event
        List<MapNodeData> candidates = new List<MapNodeData>();
        foreach (var node in sideNodes)
        {
            if (node.encounterType == EncounterType.Normal)
                candidates.Add(node);
        }

        int toAssign = Mathf.Min(targetCount, candidates.Count);
        for (int i = 0; i < toAssign; i++)
        {
            int idx = rng.Next(candidates.Count);
            candidates[idx].encounterType = type;
            candidates.RemoveAt(idx);
        }
    }

    // --------------------------------------------------------------------
    // Option A: REPAIR-LITE
    // --------------------------------------------------------------------

    private void ApplyRepairLite(MapGraph graph, List<MapNodeData> sideNodes, System.Random rng)
    {
        if (sideNodes.Count == 0)
            return;

        for (int iter = 0; iter < MaxRepairIterations; iter++)
        {
            CountEncounters(graph,
                            out int eliteCount,
                            out int eventCount,
                            out int shopCount,
                            out int specialCount);

            bool countsOk =
                IsWithinWithTolerance(eliteCount, ElitesMin, ElitesMax) &&
                IsWithinWithTolerance(eventCount, EventsMin, EventsMax) &&
                IsWithinWithTolerance(shopCount, ShopsMin, ShopsMax) &&
                IsWithinWithTolerance(specialCount, SpecialsMin, SpecialsMax);

            bool shopChainBad = HasShopRowChainOf3OrMore(graph);

            if (countsOk && !shopChainBad)
                break; // we are within tolerances and shop chain is valid

            // 1) First priority: fix shop chains of length  3
            if (shopChainBad && shopCount > 0)
            {
                var offendingRows = GetShopChainRows(graph);
                var offendingShops = new List<MapNodeData>();

                foreach (var node in sideNodes)
                {
                    if (node.encounterType == EncounterType.Shop &&
                        offendingRows.Contains(node.rowIndex))
                    {
                        offendingShops.Add(node);
                    }
                }

                if (offendingShops.Count > 0)
                {
                    var pick = offendingShops[rng.Next(offendingShops.Count)];
                    // Simple fix: downgrade shop  event
                    pick.encounterType = EncounterType.Event;
                    continue;
                }
            }

            // 2) Fix counts (increment/decrement types slightly)
            var needMore = new List<EncounterType>();
            var needLess = new List<EncounterType>();

            if (eliteCount < ElitesMin - Tolerance) needMore.Add(EncounterType.Elite);
            if (eventCount < EventsMin - Tolerance) needMore.Add(EncounterType.Event);
            if (shopCount < ShopsMin - Tolerance) needMore.Add(EncounterType.Shop);
            if (specialCount < SpecialsMin - Tolerance) needMore.Add(EncounterType.Special);

            if (eliteCount > ElitesMax + Tolerance) needLess.Add(EncounterType.Elite);
            if (eventCount > EventsMax + Tolerance) needLess.Add(EncounterType.Event);
            if (shopCount > ShopsMax + Tolerance) needLess.Add(EncounterType.Shop);
            if (specialCount > SpecialsMax + Tolerance) needLess.Add(EncounterType.Special);

            if (needMore.Count == 0 && needLess.Count == 0)
                break; // nothing to fix

            EncounterType incType = needMore.Count > 0
                ? needMore[rng.Next(needMore.Count)]
                : EncounterType.Event;

            EncounterType decType = needLess.Count > 0
                ? needLess[rng.Next(needLess.Count)]
                : EncounterType.Event;

            if (incType == decType)
                continue;

            // Find side node to change from decType  incType
            var decCandidates = new List<MapNodeData>();
            foreach (var node in sideNodes)
            {
                if (node.encounterType == decType)
                    decCandidates.Add(node);
            }

            if (decCandidates.Count == 0)
                continue;

            var chosen = decCandidates[rng.Next(decCandidates.Count)];
            chosen.encounterType = incType;
        }
    }

    // --------------------------------------------------------------------
    // Helpers
    // --------------------------------------------------------------------

    private List<MapNodeData> GetSideNodes(MapGraph graph)
    {
        var result = new List<MapNodeData>();
        int lastRow = graph.totalRows - 1;

        for (int r = 0; r < graph.rows.Count; r++)
        {
            if (r == 0 || r == lastRow)
                continue; // skip start & boss rows

            foreach (var node in graph.rows[r])
                result.Add(node);
        }

        return result;
    }

    private void CountEncounters(MapGraph graph,
                                 out int eliteCount,
                                 out int eventCount,
                                 out int shopCount,
                                 out int specialCount)
    {
        eliteCount = eventCount = shopCount = specialCount = 0;

        for (int r = 0; r < graph.rows.Count; r++)
        {
            foreach (var node in graph.rows[r])
            {
                switch (node.encounterType)
                {
                    case EncounterType.Elite: eliteCount++; break;
                    case EncounterType.Event: eventCount++; break;
                    case EncounterType.Shop: shopCount++; break;
                    case EncounterType.Special: specialCount++; break;
                }
            }
        }
    }

    private bool IsWithinWithTolerance(int value, int min, int max)
    {
        return value >= (min - Tolerance) && value <= (max + Tolerance);
    }

    /// <summary>
    /// Returns true if there is at least one chain of  3 consecutive rows
    /// that each have at least one Shop.
    /// </summary>
    private bool HasShopRowChainOf3OrMore(MapGraph graph)
    {
        int rowCount = graph.totalRows;
        if (rowCount < 3) return false;

        bool[] rowHasShop = new bool[rowCount];

        for (int r = 0; r < graph.rows.Count; r++)
        {
            foreach (var node in graph.rows[r])
            {
                if (node.encounterType == EncounterType.Shop)
                {
                    rowHasShop[r] = true;
                    break;
                }
            }
        }

        int streak = 0;
        for (int r = 0; r < rowHasShop.Length; r++)
        {
            if (rowHasShop[r]) streak++;
            else streak = 0;

            if (streak >= 3)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Returns all row indices that are part of any 3+ Shop-row chain.
    /// Used to pick shops to downgrade in repair.
    /// </summary>
    private HashSet<int> GetShopChainRows(MapGraph graph)
    {
        var rows = new HashSet<int>();
        int rowCount = graph.totalRows;
        if (rowCount < 3) return rows;

        bool[] rowHasShop = new bool[rowCount];

        for (int r = 0; r < graph.rows.Count; r++)
        {
            foreach (var node in graph.rows[r])
            {
                if (node.encounterType == EncounterType.Shop)
                {
                    rowHasShop[r] = true;
                    break;
                }
            }
        }

        int streak = 0;
        for (int r = 0; r < rowHasShop.Length; r++)
        {
            if (rowHasShop[r]) streak++;
            else streak = 0;

            if (streak >= 3)
            {
                for (int k = 0; k < streak; k++)
                    rows.Add(r - k);
            }
        }

        return rows;
    }
}
