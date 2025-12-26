using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// EncounterGeneratorOptionA
/// Ansvar: Påfører "Option A" regler for encounter-tildeling på en allerede genereret MapGraph.
/// - Regelbaseret fordeling af Elites, Events, Shops og Specials inden for specificerede intervaller.
/// - Efterfulgt af en "repair-lite" fase som justerer enkelte noder for at komme inden for tolerance.
/// - Returnerer et pass/fail flag der angiver om slutresultatet overholder målene og shop-kæderegler.
/// Klassen er en ren hjælper (ikke ScriptableObject) og opererer direkte på MapGraph data.
/// </summary>
public class EncounterGeneratorOptionA
{
    // Målintervaller for antal pr. map
    private const int ElitesMin = 7;
    private const int ElitesMax = 9;

    private const int EventsMin = 10;
    private const int EventsMax = 14;

    private const int ShopsMin = 5;
    private const int ShopsMax = 7;

    private const int SpecialsMin = 4;
    private const int SpecialsMax = 6;

    // Tolerance for endelige totals (± Tolerance)
    private const int Tolerance = 1;

    // Maks antal iterationer i repair-fasen for at undgå uendelige loops
    private const int MaxRepairIterations = 200;

    /// <summary>
    /// Hovedmetode: anvender Option A regler og repair på grafen.
    /// - Opdaterer encounterType på MapNodeData for side-noder (ikke start eller boss).
    /// - 'pass' sættes kun hvis totals inden for tolerance og ingen 3+ shop-rækker i streg.
    /// </summary>
    /// <param name="graph">MapGraph med allerede genererede rækker og noder.</param>
    /// <param name="rng">System.Random instans for reproducerbarhed.</param>
    /// <param name="pass">Out-parameter: true hvis validering bestået.</param>
    public void Generate(MapGraph graph, System.Random rng, out bool pass)
    {
        pass = false;

        if (graph == null || graph.rows == null || graph.rows.Count == 0)
            return;

        int lastRow = graph.totalRows - 1;

        // 1) Init: sæt Start/Boss/Normal for alle noder
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
            // Intet at placere på -> fail
            return;
        }

        // 2) Anvend Option A basissæt af regler
        ApplyOptionARules(graph, sideNodes, rng);

        // 3) Repair-lite: små justeringer indtil totals og shop-kæder er ok eller iterationsgrænsen nås
        ApplyRepairLite(graph, sideNodes, rng);

        // 4) Evaluer PASS/FAIL
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
    // Option A: REGELIMPLEMENTATION
    // --------------------------------------------------------------------

    /// <summary>
    /// Anvender de primære regler: vælg tilfældige mål inden for intervaller,
    /// clamp hvis der er for få side-noder, og tildel typer i prioriteret rækkefølge.
    /// </summary>
    private void ApplyOptionARules(MapGraph graph, List<MapNodeData> sideNodes, System.Random rng)
    {
        if (sideNodes.Count == 0)
            return;

        // Vælg tilfældige mål inden for definerede intervaller
        int targetElites = UnityEngine.Random.Range(ElitesMin, ElitesMax + 1);
        int targetEvents = UnityEngine.Random.Range(EventsMin, EventsMax + 1);
        int targetShops = UnityEngine.Random.Range(ShopsMin, ShopsMax + 1);
        int targetSpecials = UnityEngine.Random.Range(SpecialsMin, SpecialsMax + 1);

        // Hvis summen overstiger antal side-noder, reduceres efter prioritet
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

        // Starttilstand: alle side-noder som Normal
        foreach (var node in sideNodes)
            node.encounterType = EncounterType.Normal;

        // Tildel i prioriteret rækkefølge: Shops først (så vi kan reparere chains senere)
        AssignRandomType(sideNodes, EncounterType.Shop, targetShops, rng);

        // Elites
        AssignRandomType(sideNodes, EncounterType.Elite, targetElites, rng);

        // Specials
        AssignRandomType(sideNodes, EncounterType.Special, targetSpecials, rng);

        // Events
        AssignRandomType(sideNodes, EncounterType.Event, targetEvents, rng);
    }

    /// <summary>
    /// Hjælper: vælg tilfældige noder blandt de der stadig er Normal og sæt dem til 'type'.
    /// </summary>
    private void AssignRandomType(List<MapNodeData> sideNodes, EncounterType type, int targetCount, System.Random rng)
    {
        if (targetCount <= 0) return;

        // Kandidater er noder som endnu er Normal
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
    // Option A: REPAIR-LITE FASE
    // --------------------------------------------------------------------

    /// <summary>
    /// Små, iterative justeringer for at bringe counts og shop-kæder inden for regler.
    /// - Forsøger først at bryde 3+ shop-rækker.
    /// - Derefter foretages små inc/dec ændringer indtil tolerancer overholdes eller max-iteration nås.
    /// </summary>
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
                break; // tilfredsstillende

            // 1) Prioritet: bryd shop-kæder (3+ rækker)
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
                    // Enkel reparation: nedgrader en shop til event
                    pick.encounterType = EncounterType.Event;
                    continue;
                }
            }

            // 2) Juster counts: bestem hvilke typer der mangler / er for mange
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
                break; // intet at gøre

            EncounterType incType = needMore.Count > 0
                ? needMore[rng.Next(needMore.Count)]
                : EncounterType.Event;

            EncounterType decType = needLess.Count > 0
                ? needLess[rng.Next(needLess.Count)]
                : EncounterType.Event;

            if (incType == decType)
                continue;

            // Find en kandidat som kan nedgraderes fra decType til incType
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
    // Hjælpefunktioner
    // --------------------------------------------------------------------

    /// <summary>
    /// Returnerer alle side-noder (alle rækker undtagen start og boss).
    /// </summary>
    private List<MapNodeData> GetSideNodes(MapGraph graph)
    {
        var result = new List<MapNodeData>();
        int lastRow = graph.totalRows - 1;

        for (int r = 0; r < graph.rows.Count; r++)
        {
            if (r == 0 || r == lastRow)
                continue; // skip start & boss rækker

            foreach (var node in graph.rows[r])
                result.Add(node);
        }

        return result;
    }

    /// <summary>
    /// Optæller antal af hver relevant encounter-type i hele grafen.
    /// </summary>
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

    /// <summary>
    /// Simpelt tjek med tolerance: true hvis value ligger i [min - Tolerance, max + Tolerance].
    /// </summary>
    private bool IsWithinWithTolerance(int value, int min, int max)
    {
        return value >= (min - Tolerance) && value <= (max + Tolerance);
    }

    /// <summary>
    /// Returnerer true hvis der findes mindst en sekvens af 3 på hinanden følgende rækker
    /// hvor hver række indeholder mindst en Shop.
    /// Bruges til at undgå kedelige 'shop-stacks'.
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
    /// Returnerer alle rækkeindeks som indgår i mindst en shop-kæde af længde 3 eller mere.
    /// Hjælper repair-fasen med at finde hvilke shops der skal nedgraderes.
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
