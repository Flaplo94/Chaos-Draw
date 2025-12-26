using System;
using System.Collections.Generic;
using Unity.VisualScripting;

/// <summary>
/// Option B: For specifikke seeds anvendes et hardcoded encounter-layout på en allerede-genereret graf.
/// Layoutet er defineret per række (rækker 1..sidste-1) og per node-indeks inden for rækken (venstre->højre).
///
/// PASS hvis:
/// - seed findes i presets
/// - og hver preset-række har samme antal noder som grafens tilsvarende række (rækker 1..sidste-1)
/// Ellers FAIL (ingen ændringer ud over start/boss).
/// </summary>
public class EncounterGeneratorOptionB
{
    // Seed -> rækker -> encounters
    // Kun midter-rækker er inkluderet (række 1 .. lastRow-1). Start/Boss håndteres særskilt.
    private readonly Dictionary<int, EncounterType[][]> presets = new()
    {

        // Eksempel-placeringer (ERSTAT med dine reelle layouts):
        // Række-arrays skal have PRÆCIS samme node-antal som de genererede graf-rækker.
        // Orden er venstre->højre inde i hver række.
        { 111, new EncounterType[][]
            {
                // række 1
                new [] { EncounterType.Normal, EncounterType.Normal, EncounterType.Event, EncounterType.Normal },
                // række 2
                new [] { EncounterType.Normal, EncounterType.Elite },
                // række 3
                new [] { EncounterType.Normal, EncounterType.Event, EncounterType.Shop },
                // række 4
                new [] { EncounterType.Normal, EncounterType.Event },
                // række 5
                new [] { EncounterType.Elite, EncounterType.Normal, EncounterType.Elite },
                // række 6
                new [] { EncounterType.Event, EncounterType.Normal, EncounterType.Normal, EncounterType.Event },
                // række 7
                new [] { EncounterType.Normal, EncounterType.Elite, EncounterType.Elite },
                // række 8
                new [] { EncounterType.Normal, EncounterType.Event, EncounterType.Shop, EncounterType.Normal },
                // række 9
                new [] { EncounterType.Normal, EncounterType.Event, EncounterType.Normal },
                // række 10
                new [] { EncounterType.Normal, EncounterType.Elite, EncounterType.Event },
                // række 11
                new [] { EncounterType.Normal, EncounterType.Normal, EncounterType.Normal, EncounterType.Normal },
                // række 12
                new [] { EncounterType.Event, EncounterType.Shop, EncounterType.Normal },
                // række 13
                new [] { EncounterType.Normal, EncounterType.Event },
            }
        },

        { 222, new EncounterType[][]
            {
                // række 1
                new [] { EncounterType.Normal, EncounterType.Normal, EncounterType.Event, EncounterType.Normal },
                // række 2
                new [] { EncounterType.Normal, EncounterType.Event },
                // række 3
                new [] { EncounterType.Shop, EncounterType.Event },
                // række 4
                new [] { EncounterType.Normal, EncounterType.Event, EncounterType.Normal },
                // række 5
                new [] { EncounterType.Normal, EncounterType.Event, EncounterType.Elite },
                // række 6
                new [] { EncounterType.Normal, EncounterType.Event, EncounterType.Shop, EncounterType.Event },
                // række 7
                new [] { EncounterType.Normal, EncounterType.Event, EncounterType.Elite },
                // række 8
                new [] { EncounterType.Normal, EncounterType.Event },
                // række 9
                new [] { EncounterType.Normal, EncounterType.Event, EncounterType.Event, EncounterType.Shop },
                // række 10
                new [] { EncounterType.Normal, EncounterType.Event, EncounterType.Normal, EncounterType.Elite },
                // række 11
                new [] { EncounterType.Normal, EncounterType.Event },
                // række 12
                new [] { EncounterType.Normal, EncounterType.Event, EncounterType.Elite },
                // række 13
                new [] { EncounterType.Normal, EncounterType.Event, EncounterType.Elite, EncounterType.Normal },
            }
        },

        { 333, new EncounterType[][]
            {
                // række 1
                new [] { EncounterType.Normal, EncounterType.Normal, EncounterType.Event, EncounterType.Elite },
                // række 2
                new [] { EncounterType.Normal, EncounterType.Event, EncounterType.Normal, EncounterType.Elite },
                // række 3
                new [] { EncounterType.Normal, EncounterType.Elite },
                // række 4
                new [] { EncounterType.Normal, EncounterType.Event },
                // række 5
                new [] { EncounterType.Normal, EncounterType.Normal, EncounterType.Elite, EncounterType.Shop },
                // række 6
                new [] { EncounterType.Normal, EncounterType.Event, EncounterType.Elite },
                // række 7
                new [] { EncounterType.Normal, EncounterType.Elite, EncounterType.Elite },
                // række 8
                new [] { EncounterType.Normal, EncounterType.Elite, EncounterType.Shop },
                // række 9
                new [] { EncounterType.Normal, EncounterType.Event, EncounterType.Elite },
                // række 10
                new [] { EncounterType.Normal, EncounterType.Normal, EncounterType.Elite, EncounterType.Event },
                // række 11
                new [] { EncounterType.Normal, EncounterType.Elite, EncounterType.Elite },
                // række 12
                new [] { EncounterType.Normal, EncounterType.Event, EncounterType.Shop },
                // række 13
                new [] { EncounterType.Normal, EncounterType.Elite },
            }
        },
    };

    /// <summary>
    /// Anvend preset-layout for et givent seed.
    /// </summary>
    /// <param name="graph">Kortets grafmodel (MapGraph) som allerede er opbygget.</param>
    /// <param name="seed">Seed der bruges til opslag i preset-dictionary.</param>
    /// <param name="pass">Out: true hvis preset blev anvendt succesfuldt.</param>
    /// <param name="failReason">Out: forklaring ved fejl (brugbar i debug/log).</param>
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
        int middleRowCount = lastRow - 1; // rækker 1..last-1
        // Sæt eksplicit Start og Boss
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
        // Valider antal rækker i preset
        if (layout.Length != middleRowCount)
        {
            failReason = $"Preset rows={layout.Length} but graph middle rows={middleRowCount}";
            return;
        }

        // Valider at hver rækkes node-antal matcher og anvend preset
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

            // Anvend encounters fra preset venstre->højre
            for (int i = 0; i < rowNodes.Count; i++)
            {
                rowNodes[i].encounterType = presetRow[i];
            }
        }

        pass = true;
    }
}
