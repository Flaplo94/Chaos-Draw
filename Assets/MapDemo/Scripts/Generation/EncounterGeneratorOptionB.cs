using System;
using System.Collections.Generic;
using Unity.VisualScripting;

/// <summary>
/// Option B: For specific seeds, apply a hardcoded encounter layout to the already-generated graph.
/// The layout is per row (rows 1..last-1), and per node index within each row (left-to-right).
///
/// PASS if:
/// - seed exists in presets
/// - and each row's preset count matches the graph row node count (rows 1..last-1)
/// Otherwise FAIL (no changes beyond defaults).
/// </summary>
public class EncounterGeneratorOptionB
{
    // Seed -> rows -> encounters
    // Only middle rows are included (row 1 .. lastRow-1). Start/Boss are handled elsewhere.
    private readonly Dictionary<int, EncounterType[][]> presets = new()
    {

        // Example placeholders (REPLACE with your real layouts):
        // Row arrays must have EXACTLY the same node count as the generated graph rows.
        // The order is left-to-right index inside each row.
        { 111, new EncounterType[][]
            {
                // row 1
                new [] { EncounterType.Normal, EncounterType.Normal, EncounterType.Event, EncounterType.Normal },
                // row 2
                new [] { EncounterType.Normal, EncounterType.Elite },
                // ...
                new [] { EncounterType.Normal, EncounterType.Event, EncounterType.Shop },

                new [] { EncounterType.Normal, EncounterType.Event },

                new [] { EncounterType.Elite, EncounterType.Normal, EncounterType.Elite },

                new [] { EncounterType.Event, EncounterType.Normal, EncounterType.Normal, EncounterType.Event },

                new [] { EncounterType.Normal, EncounterType.Elite, EncounterType.Elite },

                new [] { EncounterType.Normal, EncounterType.Event, EncounterType.Shop, EncounterType.Normal },

                new [] { EncounterType.Normal, EncounterType.Event, EncounterType.Normal },

                new [] { EncounterType.Normal, EncounterType.Elite, EncounterType.Event },

                new [] { EncounterType.Normal, EncounterType.Normal, EncounterType.Normal, EncounterType.Normal },

                new [] { EncounterType.Event, EncounterType.Shop, EncounterType.Normal },

                new [] { EncounterType.Normal, EncounterType.Event },

            }
        },

        { 222, new EncounterType[][]
            {
                // row 1
                new [] { EncounterType.Normal, EncounterType.Normal, EncounterType.Event, EncounterType.Normal },
                // row 2
                new [] { EncounterType.Normal, EncounterType.Event },
                // ...
                new [] { EncounterType.Shop,  EncounterType.Event },

                new [] { EncounterType.Normal, EncounterType.Event, EncounterType.Normal },

                new [] { EncounterType.Normal, EncounterType.Event, EncounterType.Elite },

                new [] { EncounterType.Normal, EncounterType.Event, EncounterType.Shop, EncounterType.Event },

                new [] { EncounterType.Normal, EncounterType.Event, EncounterType.Elite },

                new [] { EncounterType.Normal, EncounterType.Event },

                new [] { EncounterType.Normal, EncounterType.Event, EncounterType.Event, EncounterType.Shop },

                new [] { EncounterType.Normal, EncounterType.Event, EncounterType.Normal, EncounterType.Elite },

                new [] { EncounterType.Normal, EncounterType.Event },

                new [] { EncounterType.Normal, EncounterType.Event, EncounterType.Elite },

                new [] { EncounterType.Normal, EncounterType.Event, EncounterType.Elite, EncounterType.Normal },
            }
        },

        { 333, new EncounterType[][]
            {
                // row 1
                new [] { EncounterType.Normal, EncounterType.Normal, EncounterType.Event, EncounterType.Elite },
                // row 2
                new [] { EncounterType.Normal, EncounterType.Event, EncounterType.Normal, EncounterType.Elite },
                // ...
                new [] { EncounterType.Normal,  EncounterType.Elite },

                new [] { EncounterType.Normal, EncounterType.Event },

                new [] { EncounterType.Normal, EncounterType.Normal, EncounterType.Elite, EncounterType.Shop },

                new [] { EncounterType.Normal, EncounterType.Event, EncounterType.Elite },

                new [] { EncounterType.Normal, EncounterType.Elite, EncounterType.Elite },

                new [] { EncounterType.Normal, EncounterType.Elite, EncounterType.Shop },

                new [] { EncounterType.Normal, EncounterType.Event, EncounterType.Elite },

                new [] { EncounterType.Normal, EncounterType.Normal, EncounterType.Elite, EncounterType.Event },

                new [] { EncounterType.Normal, EncounterType.Elite, EncounterType.Elite },

                new [] { EncounterType.Normal, EncounterType.Event, EncounterType.Shop },

                new [] { EncounterType.Normal, EncounterType.Elite },
            }
        },
    };

    public void Generate(MapGraph graph, int seed, out bool pass, out string failReason)
    {
        pass = false;
        failReason = "";

        if (graph == null || graph.rows == null || graph.rows.Count == 0)
        {
            failReason = "Graph missing";
            return;
        }

        if (!presets.TryGetValue(seed, out var layout))
        {
            failReason = $"No preset for seed {seed}";
            return;
        }

        int lastRow = graph.totalRows - 1;
        int middleRowCount = lastRow - 1; // rows 1..last-1
        // Explicitly assign Start and Boss
        for (int r = 0; r < graph.rows.Count; r++)
        {
            foreach (var node in graph.rows[r])
            {
                if (r == 0)
                    node.encounterType = EncounterType.Start;
                else if (r == lastRow)
                    node.encounterType = EncounterType.Boss;
            }
        }
        // Validate row count
        if (layout.Length != middleRowCount)
        {
            failReason = $"Preset rows={layout.Length} but graph middle rows={middleRowCount}";
            return;
        }

        // Validate each row node count matches and apply
        for (int r = 1; r < lastRow; r++)
        {
            var rowNodes = graph.rows[r];
            var presetRow = layout[r - 1];

            if (rowNodes == null)
            {
                failReason = $"Graph row {r} is null";
                return;
            }

            if (presetRow.Length != rowNodes.Count)
            {
                failReason = $"Row {r} mismatch: preset={presetRow.Length} graph={rowNodes.Count}";
                return;
            }

            // Apply encounters left-to-right
            for (int i = 0; i < rowNodes.Count; i++)
            {
                rowNodes[i].encounterType = presetRow[i];
            }
        }

        pass = true;
    }
}
