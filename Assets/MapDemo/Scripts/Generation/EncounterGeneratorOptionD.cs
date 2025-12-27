using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Option D = Option C (budget per row) + rule constraints:
/// 1) No 3 of the same encounter type in a row along any path (consecutive rows).
/// 2) No Elite in row 1 (row just after Start).
/// 3) Split the map into 3 parts (start/mid/end) and ensure EACH part contains at least:
///    - 1 Shop
///    - 2 Events
///    - 1 Elite
///
/// NOTE:
/// - This script only assigns encounters to already-generated nodes in MapGraph.
/// - It respects the row budgets in OptionCSettings with tolerance ±1 (same as Option C).
/// - It uses a repair loop to satisfy rules while staying within row budgets.
/// </summary>
public class EncounterGeneratorOptionD
{
    private const int MaxRepairIterations = 400;

    // Per-part minimums
    private const int MinShopPerPart = 1;
    private const int MinEventsPerPart = 2;
    private const int MinElitePerPart = 1;

    public void Generate(
    MapGraph graph,
    System.Random rng,
    OptionCSettings settings,
    out bool pass)
    {
        pass = false;

        if (graph == null || graph.rows == null || graph.rows.Count == 0 || settings == null)
        {
            Debug.LogWarning("[OptionD] Invalid inputs");
            return;
        }

        int lastRow = graph.totalRows - 1;

        var idToNode = BuildIdLookup(graph);
        var incoming = BuildIncomingAdjacency(graph, idToNode);

        // ------------------------------------------------------------
        // 1) Init Start / Boss / Normal
        // ------------------------------------------------------------
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

        // ------------------------------------------------------------
        // 2) Initial per-row assignment by budget (Option C style)
        // ------------------------------------------------------------
        bool allRowsHadSolution = true;

        for (int r = 1; r < lastRow; r++)
        {
            var rowNodes = graph.rows[r];
            if (rowNodes == null || rowNodes.Count == 0)
                continue;

            int budget = GetRowBudget(settings, r);
            if (budget <= 0)
            {
                allRowsHadSolution = false;
                continue;
            }

            bool rowPass = AssignRowByBudget(
                graph,
                rowNodes,
                r,
                budget,
                rng,
                settings,
                forbidElite: (r == 1)
            );

            if (!rowPass)
                allRowsHadSolution = false;
        }

        // DEBUG: after initial assignment
        DumpAllRowsBudgetDebug(graph, settings, "OptionD-Init");

        // ------------------------------------------------------------
        // 3) Repair loop (rules enforcement)
        // ------------------------------------------------------------
        for (int iter = 0; iter < MaxRepairIterations; iter++)
        {
            // DEBUG: see evolution during repair
            if (iter == 0 || iter == 25 || iter == 50 || iter == 100)
                DumpAllRowsBudgetDebug(graph, settings, $"OptionD-Repair{iter}");

            // Rule 2: no Elite in row 1
            if (RowHasType(graph.rows[1], EncounterType.Elite))
            {
                bool fixedRow1 = TryFixRow(
                    graph,
                    rng,
                    settings,
                    rowIndex: 1,
                    incoming,
                    idToNode,
                    desiredType: null,
                    forbidElite: true
                );

                if (!fixedRow1)
                {
                    Debug.LogWarning("[OptionD] FAIL: could not remove Elite from row 1");
                    break;
                }

                continue;
            }

            // Rule 3: per-part minimums
            var parts = SplitIntoThreeParts(graph.totalRows);
            var deficits = ComputePartDeficits(graph, parts);

            // Rule 1: streaks (non-Normal only)
            var streakOffenders = FindStreak3Offenders(graph, incoming);

            bool okDeficits = deficits.totalMissing == 0;
            bool okStreaks = streakOffenders.Count == 0;

            if (okDeficits && okStreaks)
            {
                Debug.Log($"[OptionD] PASS reached. allRowsHadSolution={allRowsHadSolution}");

                if (!allRowsHadSolution)
                    Debug.LogWarning("[OptionD] PASS, but initial assignment had at least one row with no valid combos.");

                pass = true;
                return;
            }

            // --------------------------------------------------------
            // Fix deficits first
            // --------------------------------------------------------
            if (!okDeficits)
            {
                bool repaired = TryRepairDeficit(
                    graph,
                    rng,
                    settings,
                    parts,
                    deficits,
                    incoming,
                    idToNode
                );

                if (!repaired)
                {
                    Debug.LogWarning($"[OptionD] FAIL: deficits remain (missing={deficits.totalMissing})");
                    break;
                }

                continue;
            }

            // --------------------------------------------------------
            // Fix streak offenders
            // --------------------------------------------------------
            if (!okStreaks)
            {
                var offender = streakOffenders[rng.Next(streakOffenders.Count)];

                bool fixedStreak = TryFixRow(
                    graph,
                    rng,
                    settings,
                    offender.rowIndex,
                    incoming,
                    idToNode,
                    desiredType: null,
                    forbidElite: (offender.rowIndex == 1)
                );

                if (!fixedStreak)
                {
                    Debug.LogWarning("[OptionD] FAIL: could not break streak");
                    break;
                }

                continue;
            }
        }

        // ------------------------------------------------------------
        // If we get here, repair failed
        // ------------------------------------------------------------
        pass = false;
        Debug.LogWarning("[OptionD] FAIL: exceeded repair attempts");
    }


    // ------------------------------------------------------------
    // Repair helpers
    // ------------------------------------------------------------

    private bool TryRepairDeficit(
        MapGraph graph,
        System.Random rng,
        OptionCSettings settings,
        (int startRow, int endRow)[] parts,
        PartDeficits deficits,
        Dictionary<int, List<int>> incoming,
        Dictionary<int, MapNodeData> idToNode)
    {
        // Try to repair in a deterministic priority order: Shop -> Elite -> Events
        // (You can change priority if you want)
        for (int p = 0; p < 3; p++)
        {
            if (deficits.missingShop[p] > 0)
            {
                if (TryAddTypeToPart(graph, rng, settings, parts[p], EncounterType.Shop, incoming, idToNode))
                    return true;
            }

            if (deficits.missingElite[p] > 0)
            {
                // Rule 2 forbids elite in row 1; handle naturally by row constraints
                if (TryAddTypeToPart(graph, rng, settings, parts[p], EncounterType.Elite, incoming, idToNode))
                    return true;
            }

            if (deficits.missingEvents[p] > 0)
            {
                if (TryAddTypeToPart(graph, rng, settings, parts[p], EncounterType.Event, incoming, idToNode))
                    return true;
            }
        }

        return false;
    }

    private bool TryAddTypeToPart(
        MapGraph graph,
        System.Random rng,
        OptionCSettings settings,
        (int startRow, int endRow) part,
        EncounterType desired,
        Dictionary<int, List<int>> incoming,
        Dictionary<int, MapNodeData> idToNode)
    {
        // Choose a random row inside the part (excluding start/boss rows already excluded by part ranges)
        var candidateRows = new List<int>();
        for (int r = part.startRow; r <= part.endRow; r++)
        {
            if (r <= 0 || r >= graph.totalRows - 1) continue;
            candidateRows.Add(r);
        }

        if (candidateRows.Count == 0) return false;

        // Try a handful of attempts (randomized)
        for (int attempt = 0; attempt < 20; attempt++)
        {
            int rowIndex = candidateRows[rng.Next(candidateRows.Count)];

            // Row 1 cannot contain Elite
            bool forbidElite = (rowIndex == 1);

            // Try to pick a new combo for that row that includes the desired type
            bool changed = TryFixRow(graph, rng, settings, rowIndex, incoming, idToNode,
                                     desiredType: desired, forbidElite: forbidElite);
            if (changed) return true;
        }

        return false;
    }

    private bool TryFixRow(
        MapGraph graph,
        System.Random rng,
        OptionCSettings settings,
        int rowIndex,
        Dictionary<int, List<int>> incoming,
        Dictionary<int, MapNodeData> idToNode,
        EncounterType? desiredType,
        bool forbidElite)
    {
        int lastRow = graph.totalRows - 1;
        if (rowIndex <= 0 || rowIndex >= lastRow) return false;

        var rowNodes = graph.rows[rowIndex];
        if (rowNodes == null || rowNodes.Count == 0) return false;

        int budget = GetRowBudget(settings, rowIndex);
        if (budget <= 0) return false;

        // Generate all valid combos for this row under budget window
        var combos = GenerateRowCombos(rowNodes.Count, budget, settings, forbidElite);

        if (desiredType.HasValue)
        {
            // Filter combos to those containing desiredType
            combos.RemoveAll(c => !ContainsType(c, desiredType.Value));
        }

        if (combos.Count == 0) return false;

        // Score combos by how many violations they cause when applied
        EncounterType[] best = null;
        int bestScore = int.MaxValue;

        // Light randomization to avoid always picking same
        Shuffle(combos, rng);

        // Evaluate a subset if list is huge (it won't be: row size max 4)
        for (int i = 0; i < combos.Count; i++)
        {
            var combo = combos[i];

            // Apply temporarily
            var old = SnapshotRow(rowNodes);
            ApplyCombo(rowNodes, combo);

            // Hard check: row1 no elite
            if (rowIndex == 1 && RowHasType(rowNodes, EncounterType.Elite))
            {
                RestoreRow(rowNodes, old);
                continue;
            }

            // Compute score: streak offenders + unmet part deficits (after this change)
            int streakPenalty = FindStreak3Offenders(graph, incoming).Count * 10;

            var parts = SplitIntoThreeParts(graph.totalRows);
            var deficits = ComputePartDeficits(graph, parts);
            int deficitPenalty = deficits.totalMissing * 20;

            // Also keep it stable: fewer changes is better
            int changePenalty = CountDifferences(old, combo);

            int score = streakPenalty + deficitPenalty + changePenalty;

            // Restore
            RestoreRow(rowNodes, old);

            if (score < bestScore)
            {
                bestScore = score;
                best = combo;
                if (bestScore == 0) break; // can't do better
            }
        }

        if (best == null) return false;

        // Apply best permanently
        ApplyCombo(rowNodes, best);
        return true;
    }

    // ------------------------------------------------------------
    // Rule checks
    // ------------------------------------------------------------

    private List<MapNodeData> FindStreak3Offenders(
    MapGraph graph,
    Dictionary<int, List<int>> incoming)
    {
        var offenders = new List<MapNodeData>();

        // nodeId -> longest non-normal streak reaching this node
        var streak = new Dictionary<int, int>();

        // Start node initializes streak = 0
        if (graph.rows[0].Count > 0)
            streak[graph.rows[0][0].id] = 0;

        int lastRow = graph.totalRows - 1;

        for (int r = 1; r <= lastRow; r++)
        {
            foreach (var node in graph.rows[r])
            {
                int best = 0;

                // Normal / Start / Boss never participate in streaks
                if (node.encounterType == EncounterType.Normal ||
                    node.encounterType == EncounterType.Start ||
                    node.encounterType == EncounterType.Boss)
                {
                    streak[node.id] = 0;
                    continue;
                }

                // Check incoming parents
                if (incoming.TryGetValue(node.id, out var parents))
                {
                    foreach (var pid in parents)
                    {
                        int parentStreak = streak.TryGetValue(pid, out var ps) ? ps : 0;
                        EncounterType parentType = FindNodeType(graph, pid);

                        int cand =
                            (parentType == node.encounterType)
                            ? parentStreak + 1
                            : 1;

                        if (cand > best)
                            best = cand;
                    }
                }
                else
                {
                    best = 1;
                }

                streak[node.id] = best;

                // Flag only non-normal streaks of length 3+
                if (best >= 3)
                    offenders.Add(node);
            }
        }

        return offenders;
    }


    private EncounterType FindNodeType(MapGraph graph, int nodeId)
    {
        for (int r = 0; r < graph.rows.Count; r++)
            for (int i = 0; i < graph.rows[r].Count; i++)
                if (graph.rows[r][i].id == nodeId)
                    return graph.rows[r][i].encounterType;

        return EncounterType.Normal;
    }

    private bool RowHasType(List<MapNodeData> row, EncounterType type)
    {
        if (row == null) return false;
        for (int i = 0; i < row.Count; i++)
            if (row[i].encounterType == type) return true;
        return false;
    }

    // ------------------------------------------------------------
    // Part splitting + deficits
    // ------------------------------------------------------------

    private (int startRow, int endRow)[] SplitIntoThreeParts(int totalRows)
    {
        // Middle rows are 1..last-1
        int lastRow = totalRows - 1;
        int first = 1;
        int lastMiddle = lastRow - 1;

        int middleCount = lastMiddle - first + 1;
        if (middleCount <= 0)
        {
            return new (int, int)[] { (1, 1), (1, 1), (1, 1) };
        }

        int baseSize = middleCount / 3;
        int rem = middleCount % 3;

        int aSize = baseSize + (rem > 0 ? 1 : 0);
        int bSize = baseSize + (rem > 1 ? 1 : 0);
        int cSize = baseSize;

        int aStart = first;
        int aEnd = aStart + aSize - 1;

        int bStart = aEnd + 1;
        int bEnd = bStart + bSize - 1;

        int cStart = bEnd + 1;
        int cEnd = lastMiddle;

        return new (int, int)[] { (aStart, aEnd), (bStart, bEnd), (cStart, cEnd) };
    }

    private PartDeficits ComputePartDeficits(MapGraph graph, (int startRow, int endRow)[] parts)
    {
        int[] shop = new int[3];
        int[] elite = new int[3];
        int[] events = new int[3];

        for (int p = 0; p < 3; p++)
        {
            var part = parts[p];
            for (int r = part.startRow; r <= part.endRow; r++)
            {
                if (r <= 0 || r >= graph.totalRows - 1) continue;

                foreach (var node in graph.rows[r])
                {
                    switch (node.encounterType)
                    {
                        case EncounterType.Shop: shop[p]++; break;
                        case EncounterType.Elite: elite[p]++; break;
                        case EncounterType.Event: events[p]++; break;
                    }
                }
            }
        }

        int[] missShop = new int[3];
        int[] missElite = new int[3];
        int[] missEvents = new int[3];

        int totalMissing = 0;

        for (int p = 0; p < 3; p++)
        {
            missShop[p] = Mathf.Max(0, MinShopPerPart - shop[p]);
            missElite[p] = Mathf.Max(0, MinElitePerPart - elite[p]);
            missEvents[p] = Mathf.Max(0, MinEventsPerPart - events[p]);

            totalMissing += missShop[p] + missElite[p] + missEvents[p];
        }

        return new PartDeficits(missShop, missElite, missEvents, totalMissing);
    }

    private readonly struct PartDeficits
    {
        public readonly int[] missingShop;
        public readonly int[] missingElite;
        public readonly int[] missingEvents;
        public readonly int totalMissing;

        public PartDeficits(int[] s, int[] e, int[] ev, int total)
        {
            missingShop = s;
            missingElite = e;
            missingEvents = ev;
            totalMissing = total;
        }
    }

    // ------------------------------------------------------------
    // Budget row combo generation (Option C style)
    // ------------------------------------------------------------

    private bool AssignRowByBudget(
        MapGraph graph,
        List<MapNodeData> rowNodes,
        int rowIndex,
        int budget,
        System.Random rng,
        OptionCSettings s,
        bool forbidElite)
    {
        var combos = GenerateRowCombos(rowNodes.Count, budget, s, forbidElite);
        if (combos.Count == 0) return false;

        // Random choice for initial assignment
        var chosen = combos[rng.Next(combos.Count)];
        ApplyCombo(rowNodes, chosen);
        return true;
    }

    private List<EncounterType[]> GenerateRowCombos(int nodeCount, int budget, OptionCSettings s, bool forbidElite)
    {
        int minAllowed = budget - 1;
        int maxAllowed = budget + 1;

        EncounterType[] options =
        {
            EncounterType.Normal,
            EncounterType.Special,
            EncounterType.Event,
            EncounterType.Shop,
            EncounterType.Elite
        };

        var current = new EncounterType[nodeCount];
        var valid = new List<EncounterType[]>();

        void Search(int idx, int costSoFar)
        {
            int remaining = nodeCount - idx;
            int minPossible = costSoFar + remaining * s.costNormal;
            if (minPossible > maxAllowed) return;

            if (idx == nodeCount)
            {
                if (costSoFar >= minAllowed && costSoFar <= maxAllowed)
                {
                    var combo = new EncounterType[nodeCount];
                    Array.Copy(current, combo, nodeCount);
                    valid.Add(combo);
                }
                return;
            }

            for (int i = 0; i < options.Length; i++)
            {
                var t = options[i];
                if (forbidElite && t == EncounterType.Elite) continue;

                int c = GetCost(t, s);
                int next = costSoFar + c;
                if (next > maxAllowed) continue;

                current[idx] = t;
                Search(idx + 1, next);
            }
        }

        Search(0, 0);
        return valid;
    }

    private int GetRowBudget(OptionCSettings s, int rowIndex)
    {
        if (s.rowBudgets == null || s.rowBudgets.Length == 0) return 0;
        if (rowIndex < 0) rowIndex = 0;
        if (rowIndex >= s.rowBudgets.Length) rowIndex = s.rowBudgets.Length - 1;
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
            default: return s.costNormal;
        }
    }

    // ------------------------------------------------------------
    // Graph adjacency helpers
    // ------------------------------------------------------------

    private Dictionary<int, MapNodeData> BuildIdLookup(MapGraph graph)
    {
        var dict = new Dictionary<int, MapNodeData>();
        for (int r = 0; r < graph.rows.Count; r++)
        {
            foreach (var n in graph.rows[r])
                dict[n.id] = n;
        }
        return dict;
    }

    private Dictionary<int, List<int>> BuildIncomingAdjacency(MapGraph graph, Dictionary<int, MapNodeData> idToNode)
    {
        var incoming = new Dictionary<int, List<int>>();
        for (int r = 0; r < graph.rows.Count; r++)
        {
            foreach (var n in graph.rows[r])
                incoming[n.id] = new List<int>();
        }

        // Expect MapGraph to expose edges list with fromNodeId / toNodeId
        foreach (var e in graph.edges)
        {
            if (!incoming.TryGetValue(e.toNodeId, out var list))
                incoming[e.toNodeId] = list = new List<int>();

            list.Add(e.fromNodeId);
        }

        return incoming;
    }

    // ------------------------------------------------------------
    // Small utilities
    // ------------------------------------------------------------

    private EncounterType[] SnapshotRow(List<MapNodeData> rowNodes)
    {
        var old = new EncounterType[rowNodes.Count];
        for (int i = 0; i < rowNodes.Count; i++)
            old[i] = rowNodes[i].encounterType;
        return old;
    }

    private void RestoreRow(List<MapNodeData> rowNodes, EncounterType[] old)
    {
        for (int i = 0; i < rowNodes.Count && i < old.Length; i++)
            rowNodes[i].encounterType = old[i];
    }

    private void ApplyCombo(List<MapNodeData> rowNodes, EncounterType[] combo)
    {
        for (int i = 0; i < rowNodes.Count && i < combo.Length; i++)
            rowNodes[i].encounterType = combo[i];
    }

    private int CountDifferences(EncounterType[] old, EncounterType[] combo)
    {
        int diff = 0;
        for (int i = 0; i < old.Length && i < combo.Length; i++)
            if (old[i] != combo[i]) diff++;
        return diff;
    }

    private bool ContainsType(EncounterType[] combo, EncounterType t)
    {
        for (int i = 0; i < combo.Length; i++)
            if (combo[i] == t) return true;
        return false;
    }

    private void Shuffle<T>(List<T> list, System.Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private void DumpAllRowsBudgetDebug(MapGraph graph, OptionCSettings settings, string tag)
    {
        if (graph == null || graph.rows == null || settings == null) return;

        int lastRow = graph.totalRows - 1;

        for (int r = 1; r < lastRow; r++)
        {
            var row = graph.rows[r];
            if (row == null || row.Count == 0) continue;

            int budget = GetRowBudget(settings, r);
            int minAllowed = budget - 1;
            int maxAllowed = budget + 1;

            int cost = 0;
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            for (int i = 0; i < row.Count; i++)
            {
                var t = row[i].encounterType;
                cost += GetCost(t, settings);

                sb.Append(t);
                if (i < row.Count - 1) sb.Append(", ");
            }

            Debug.Log($"[{tag}] Row {r}: nodes={row.Count} budget={budget} cost={cost} types=[{sb}] allowed=[{minAllowed},{maxAllowed}]");
        }
    }

}
