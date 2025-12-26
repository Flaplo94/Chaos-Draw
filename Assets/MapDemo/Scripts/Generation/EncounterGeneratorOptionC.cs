using System;
using System.Collections.Generic;
using UnityEngine;

public class EncounterGeneratorOptionC
{
    /// <summary>
    /// Hovedindgang: Anvend Option C på et allerede genereret MapGraph.
    /// - Sætter først alle noder til Start/Boss/Normal.
    /// - For hver række (undtagen start og boss) indhenter vi det satte budget og
    ///   forsøger at finde en valide tildeling via ApplyBudgetToRow.
    /// - Out-parameter 'pass' sættes til false hvis noget mangler eller ingen gyldig
    ///   løsning findes for en række.
    /// </summary>
    /// <param name="graph">Kortets grafmodel (MapGraph) — skal være initialiseret.</param>
    /// <param name="rng">System.Random instans (bruges til at vælge mellem valide kombinationer).</param>
    /// <param name="settings">OptionCSettings indeholder cost per type og rowBudgets.</param>
    /// <param name="pass">Output: true hvis alle rækker kunne tildeles inden for budgettet.</param>
    public void Generate(MapGraph graph, System.Random rng, OptionCSettings settings, out bool pass)
    {
        pass = true;

        if (graph == null || graph.rows == null || graph.rows.Count == 0 || settings == null)
        {
            pass = false;
            return;
        }

        int lastRow = graph.totalRows - 1;

        // Init: sæt Start / Boss / Normal for alle noder
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

        // Behandle hver midter-række individuelt
        for (int r = 1; r < lastRow; r++)
        {
            var rowNodes = graph.rows[r];
            if (rowNodes == null || rowNodes.Count == 0)
                continue;

            int budget = GetRowBudget(settings, r);
            if (budget <= 0)
            {
                // Manglende eller ugyldigt budget for række => fejl
                pass = false;
                continue;
            }

            bool rowPass = ApplyBudgetToRow(rowNodes, budget, rng, settings);
            if (!rowPass)
                pass = false;
        }
    }

    /// <summary>
    /// Forsøger at finde en kombination af <see cref="EncounterType"/> for de noder i <paramref name="rowNodes"/>
    /// sådan at summen af costs ligger i intervallet [budget-1, budget+1].
    /// Returnerer true og anvender en tilfældigt valgt gyldig kombination hvis en findes.
    /// </summary>
    /// <param name="rowNodes">Listen af noder i rækken der skal tildeles typer.</param>
    /// <param name="budget">Budgetpunkt for rækken (se <see cref="OptionCSettings.rowBudgets"/>).</param>
    /// <param name="rng">Tilfældighedskilde til valg blandt gyldige kombinationer.</param>
    /// <param name="s">OptionCSettings med cost-værdier per encounter-type.</param>
    /// <returns>True hvis mindst en gyldig kombination blev fundet og anvendt; ellers false.</returns>
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

        // Rekursiv søgefunktion med pruning baseret på mindste mulig cost for resterende positioner.
        void Search(int idx, int costSoFar)
        {
            int remaining = n - idx;
            int minPossible = costSoFar + remaining * s.costNormal;
            // Hvis selv den billigste udfyldning overstiger maxAllowed, kan vi prune denne gren
            if (minPossible > maxAllowed) return;

            if (idx == n)
            {
                // Fuldført kombination: accepter hvis totalt ligger inden for tolerancen
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

                // Hvis delsum allerede overskrider maxAllowed kan vi springe denne type
                if (next > maxAllowed) continue;
                current[idx] = t;
                Search(idx + 1, next);
            }
        }

        // Start søgningen
        Search(0, 0);

        // Ingen gyldige kombinationer fundet for denne række
        if (valid.Count == 0)
            return false;

        // Vælg en tilfældig gyldig kombination og anvend den på rækkerne
        var chosen = valid[rng.Next(valid.Count)];
        for (int i = 0; i < n; i++)
            rowNodes[i].encounterType = chosen[i];

        return true;
    }

    /// <summary>
    /// Henter budget for en given række fra <see cref="OptionCSettings.rowBudgets"/>.
    /// Clamper rowIndex til arrayets bounds hvis nødvendigt.
    /// </summary>
    /// <param name="s">OptionCSettings indeholdende rowBudgets.</param>
    /// <param name="rowIndex">Rækkeindeks der forespørges.</param>
    /// <returns>Budget for rækken, eller 0 hvis rowBudgets mangler/er tomt.</returns>
    private int GetRowBudget(OptionCSettings s, int rowIndex)
    {
        if (s.rowBudgets == null || s.rowBudgets.Length == 0)
            return 0;

        if (rowIndex < 0) rowIndex = 0;
        if (rowIndex >= s.rowBudgets.Length)
            rowIndex = s.rowBudgets.Length - 1;

        return s.rowBudgets[rowIndex];
    }

    /// <summary>
    /// Returnerer cost (point) for en given <see cref="EncounterType"/> baseret på <paramref name="s"/>.
    /// </summary>
    /// <param name="type">EncounterType der skal vurderes.</param>
    /// <param name="s">OptionCSettings med cost værdier.</param>
    /// <returns>Cost for den angivne encounter type.</returns>
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
