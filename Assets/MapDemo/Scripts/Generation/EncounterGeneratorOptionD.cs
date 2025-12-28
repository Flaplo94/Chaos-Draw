using System;
using System.Collections.Generic;
using UnityEngine;

// Dette script er lavet af Marc
/// <summary>
/// EncounterGeneratorOptionD
/// Ansvar: Påfører "Option D" regler og en omfattende reparationsfase på en allerede genereret MapGraph.
/// - Forsøger først at finde en gyldig kombination pr. række inden for det angivne row-budget.
/// - Kører en reparationssløjfe (flere iterationer) for at rette deficits (shops, elites, events)
///   og bryde uønskede 3+ streaks langs grafens baner.
/// - Indeholder heuristikker til lokale justeringer (fx forbydelse af Elite i række 1 i visse trin)
///   og tilpasning af kombinationer ud fra en score (streak-penalty, deficit-penalty, change-penalty).
/// Klassen opererer in-place på MapGraph og returnerer et pass/fail-flag der angiver om kravene blev opfyldt.
/// </summary>
public class EncounterGeneratorOptionD
{
    // Maks antal repair-iterationer for at undgå uendelige loops
    private const int MaxRepairIterations = 400;

    // Minimumskrav per "part" (tre dele af kortet)
    private const int MinShopPerPart = 1;
    private const int MinEventsPerPart = 2;
    private const int MinElitePerPart = 1;

    /// <summary>
    /// Hovedmetode: kør Option D generatoren.
    /// - Sætter Start/Boss/Normal initialt.
    /// - Tildeler hver række et første valid combo ift. budget.
    /// - Kører en reparationssløjfe der arbejder på deficits og 3+ streaks.
    /// </summary>
    /// <param name="graph">Kortets grafmodel (MapGraph) der allerede er opbygget.</param>
    /// <param name="rng">System.Random til tilfældige valg og shuffle.</param>
    /// <param name="settings">OptionCSettings indeholder cost-værdier og rowBudgets.</param>
    /// <param name="pass">Out: true hvis generatoren lykkes inden for reparationsgrænsen.</param>
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

        // Hjælpeopslag: id -> node og incoming adjacency
        var idToNode = BuildIdLookup(graph);
        var incoming = BuildIncomingAdjacency(graph, idToNode);

        // Initialiser alle noder: Start / Boss / Normal
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

        bool allRowsHadSolution = true;

        // Første pass: forsøg at tildele hver række et combo inden for budget
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
                forbidElite: (r == 1) // forbyd elite i række 1 ved første tildeling
            );

            if (!rowPass)
                allRowsHadSolution = false;
        }

        // Debug dump initial budgets/assignments
        DumpAllRowsBudgetDebug(graph, settings, "OptionD-Init");

        // Reparationsfase: gentagne iterationer for at rette deficits og streaks
        for (int iter = 0; iter < MaxRepairIterations; iter++)
        {
            if (iter == 0 || iter == 25 || iter == 50 || iter == 100)
                DumpAllRowsBudgetDebug(graph, settings, $"OptionD-Repair{iter}");

            // Hvis række 1 indeholder Elite, forsøg at fjerne dem først (særregel)
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

            // Split kortet i tre dele og beregn mangler pr del
            var parts = SplitIntoThreeParts(graph.totalRows);
            var deficits = ComputePartDeficits(graph, parts);

            // Find noder der skaber 3+ streaks (offenders)
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

            // Prioritet: reparer deficits først
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

            // Hvis der er streaks: forsøg at bryde en tilfældig offender-række
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

        pass = false;
        Debug.LogWarning("[OptionD] FAIL: exceeded repair attempts");
    }

    /// <summary>
    /// Forsøg at reparere samlede deficits ved at tilføje manglende typer i delene.
    /// Returnerer true hvis en enkelt ændring blev foretaget.
    /// </summary>
    private bool TryRepairDeficit(
        MapGraph graph,
        System.Random rng,
        OptionCSettings settings,
        (int startRow, int endRow)[] parts,
        PartDeficits deficits,
        Dictionary<int, List<int>> incoming,
        Dictionary<int, MapNodeData> idToNode)
    {
        for (int p = 0; p < 3; p++)
        {
            if (deficits.missingShop[p] > 0)
            {
                if (TryAddTypeToPart(graph, rng, settings, parts[p], EncounterType.Shop, incoming, idToNode))
                    return true;
            }

            if (deficits.missingElite[p] > 0)
            {
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

    /// <summary>
    /// Prøv at tilfældigt finde en række i 'part' hvor vi kan tilføje desired type ved at køre TryFixRow.
    /// Forsøger op til 20 tilfældige rækker i delen.
    /// </summary>
    private bool TryAddTypeToPart(
        MapGraph graph,
        System.Random rng,
        OptionCSettings settings,
        (int startRow, int endRow) part,
        EncounterType desired,
        Dictionary<int, List<int>> incoming,
        Dictionary<int, MapNodeData> idToNode)
    {
        var candidateRows = new List<int>();
        for (int r = part.startRow; r <= part.endRow; r++)
        {
            if (r <= 0 || r >= graph.totalRows - 1) continue;
            candidateRows.Add(r);
        }

        if (candidateRows.Count == 0) return false;

        for (int attempt = 0; attempt < 20; attempt++)
        {
            int rowIndex = candidateRows[rng.Next(candidateRows.Count)];

            bool forbidElite = (rowIndex == 1);

            bool changed = TryFixRow(graph, rng, settings, rowIndex, incoming, idToNode,
                                     desiredType: desired, forbidElite: forbidElite);
            if (changed) return true;
        }

        return false;
    }

    /// <summary>
    /// Forsøger at finde det bedste combo for en række ud fra score (streaks, deficits, ændringsomkostning).
    /// - Hvis desiredType er sat, filtreres combos til kun dem der indeholder denne type.
    /// - Hvis forbidElite er true, filtreres elite væk fra combos.
    /// </summary>
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

        var combos = GenerateRowCombos(rowNodes.Count, budget, settings, forbidElite);

        if (desiredType.HasValue)
        {
            combos.RemoveAll(c => !ContainsType(c, desiredType.Value));
        }

        if (combos.Count == 0) return false;

        EncounterType[] best = null;
        int bestScore = int.MaxValue;

        Shuffle(combos, rng);

        for (int i = 0; i < combos.Count; i++)
        {
            var combo = combos[i];

            var old = SnapshotRow(rowNodes);
            ApplyCombo(rowNodes, combo);

            // Række 1 må ikke indeholde Elite under særlige regler; spring hvis opfyldt
            if (rowIndex == 1 && RowHasType(rowNodes, EncounterType.Elite))
            {
                RestoreRow(rowNodes, old);
                continue;
            }

            int streakPenalty = FindStreak3Offenders(graph, incoming).Count * 10;

            var parts = SplitIntoThreeParts(graph.totalRows);
            var deficits = ComputePartDeficits(graph, parts);
            int deficitPenalty = deficits.totalMissing * 20;

            int changePenalty = CountDifferences(old, combo);

            int score = streakPenalty + deficitPenalty + changePenalty;

            RestoreRow(rowNodes, old);

            if (score < bestScore)
            {
                bestScore = score;
                best = combo;
                if (bestScore == 0) break; // optimal løsning fundet
            }
        }

        if (best == null) return false;

        ApplyCombo(rowNodes, best);
        return true;
    }

    /// <summary>
    /// Gennemløb grafen og returner noder som er del af 3+ streaks (samme type kæde).
    /// En "streak" beregnes ved at følge indgående kanter og akkumulere længde for matchende typer.
    /// </summary>
    private List<MapNodeData> FindStreak3Offenders(
    MapGraph graph,
    Dictionary<int, List<int>> incoming)
    {
        var offenders = new List<MapNodeData>();

        var streak = new Dictionary<int, int>();

        if (graph.rows[0].Count > 0)
            streak[graph.rows[0][0].id] = 0;

        int lastRow = graph.totalRows - 1;

        for (int r = 1; r <= lastRow; r++)
        {
            foreach (var node in graph.rows[r])
            {
                int best = 0;

                if (node.encounterType == EncounterType.Normal ||
                    node.encounterType == EncounterType.Start ||
                    node.encounterType == EncounterType.Boss)
                {
                    streak[node.id] = 0;
                    continue;
                }

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

                if (best >= 3)
                    offenders.Add(node);
            }
        }

        return offenders;
    }

    /// <summary>
    /// Find encounter type for en node-id ved at lede i graph.rows.
    /// Simpel sekventiel søgning (sufficient for små grafer).
    /// </summary>
    private EncounterType FindNodeType(MapGraph graph, int nodeId)
    {
        for (int r = 0; r < graph.rows.Count; r++)
            for (int i = 0; i < graph.rows[r].Count; i++)
                if (graph.rows[r][i].id == nodeId)
                    return graph.rows[r][i].encounterType;

        return EncounterType.Normal;
    }

    /// <summary>
    /// Returnerer true hvis rækken indeholder mindst en node af den givne type.
    /// </summary>
    private bool RowHasType(List<MapNodeData> row, EncounterType type)
    {
        if (row == null) return false;
        for (int i = 0; i < row.Count; i++)
            if (row[i].encounterType == type) return true;
        return false;
    }


    /// <summary>
    /// Split kortets midter-rækker i tre sammenhængende dele.
    /// Returnerer et array med tre tuples: (startRow, endRow).
    /// </summary>
    private (int startRow, int endRow)[] SplitIntoThreeParts(int totalRows)
    {
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

    /// <summary>
    /// Beregn hvilke typer der mangler i hver af de tre dele (shops, elites, events).
    /// Returnerer et PartDeficits objekt med mangler per del og totalMissing.
    /// </summary>
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

    /// <summary>
    /// Tildel et random valid combo for en række givet budget. Returnerer true hvis muligt.
    /// </summary>
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

        var chosen = combos[rng.Next(combos.Count)];
        ApplyCombo(rowNodes, chosen);
        return true;
    }

    /// <summary>
    /// Generer alle gyldige combos for en række (rekursiv DFS med pruning).
    /// Hvis forbidElite er true udelades Elite som valg.
    /// </summary>
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
                // Hurtig pruning: beregn mindst mulige cost hvis alle resterende er 'Normal'.
                int remaining = nodeCount - idx;
                int minPossible = costSoFar + remaining * s.costNormal;
                if (minPossible > maxAllowed) return;

                if (idx == nodeCount)
                {
                    // Valider total cost inden accept
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
                    if (next > maxAllowed) continue; // overskrider max -> skip

                    current[idx] = t;
                    Search(idx + 1, next);
                }
            }

            Search(0, 0);
            return valid;
        }

        /// <summary>
        /// Hent budget for en given række ud fra settings.
        /// - Validerer input-array længde.
        /// - Klemmer rowIndex til gyldigt interval.
        /// Returnerer 0 hvis settings eller rowBudgets er ubrugeligt.
        /// </summary>
        private int GetRowBudget(OptionCSettings s, int rowIndex)
        {
            if (s.rowBudgets == null || s.rowBudgets.Length == 0) return 0;
            if (rowIndex < 0) rowIndex = 0;
            if (rowIndex >= s.rowBudgets.Length) rowIndex = s.rowBudgets.Length - 1;
            return s.rowBudgets[rowIndex];
        }

        /// <summary>
        /// Returner point-cost for en encounter-type baseret på OptionCSettings.
        /// Mindre wrapper for centraliseret omkostningslogik.
        /// </summary>
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


        /// <summary>
        /// Byg opslagstabel (id -> MapNodeData) for hurtige opslag.
        /// - Går gennem alle rows og tilføjer hver node id som nøgle.
        /// - Overskriver eventuelt eksisterende indgang hvis id gentages (bør ikke ske i korrekt graf).
        /// Kompleksitet: O(N) i antal noder.
        /// </summary>
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

        /// <summary>
        /// Byg incoming-adjacency: for hver node id en liste af parent node-ids.
        /// - Initierer en tom liste for alle kendte node-ids (sikrer entries også for isolate nodes).
        /// - Itererer edges og fylder parent-lister.
        /// Returnerer dictionary: key = nodeId, value = liste af incoming nodeIds.
        /// Kompleksitet: O(E + N).
        /// </summary>
        private Dictionary<int, List<int>> BuildIncomingAdjacency(MapGraph graph, Dictionary<int, MapNodeData> idToNode)
        {
            var incoming = new Dictionary<int, List<int>>();
            for (int r = 0; r < graph.rows.Count; r++)
            {
                foreach (var n in graph.rows[r])
                    incoming[n.id] = new List<int>();
            }

            foreach (var e in graph.edges)
            {
                if (!incoming.TryGetValue(e.toNodeId, out var list))
                    incoming[e.toNodeId] = list = new List<int>();

                list.Add(e.fromNodeId);
            }

            return incoming;
        }

        /// <summary>
        /// Opret snapshot af en rækkes encounter-typer.
        /// - Bruges før midlertidige ændringer for senere at kunne genskabe original tilstand.
        /// Returnerer et array med typer i samme rækkefølge som rowNodes.
        /// </summary>
        private EncounterType[] SnapshotRow(List<MapNodeData> rowNodes)
        {
            var old = new EncounterType[rowNodes.Count];
            for (int i = 0; i < rowNodes.Count; i++)
                old[i] = rowNodes[i].encounterType;
            return old;
        }

        /// <summary>
        /// Genskab en rækkes encounter-typer fra et tidligere snapshot.
        /// - Sikker: limitterer til min længde af rowNodes og snapshot-array.
        /// Sideeffekt: ændrer node.encounterType in-place.
        /// </summary>
        private void RestoreRow(List<MapNodeData> rowNodes, EncounterType[] old)
        {
            for (int i = 0; i < rowNodes.Count && i < old.Length; i++)
                rowNodes[i].encounterType = old[i];
        }

        /// <summary>
        /// Anvend en combo (array af EncounterType) på en række noder.
        /// - Oversætter hver combo-element til tilsvarende node i rækken.
        /// - Begrænser opdatering til mindste fælles længde (sikker operation).
        /// </summary>
        private void ApplyCombo(List<MapNodeData> rowNodes, EncounterType[] combo)
        {
            for (int i = 0; i < rowNodes.Count && i < combo.Length; i++)
                rowNodes[i].encounterType = combo[i];
        }

        /// <summary>
        /// Tæl antal positioner hvor to arrays af EncounterType adskiller sig.
        /// - Brugt som "change penalty" ved scoring i reparationsalgoritmen.
        /// </summary>
        private int CountDifferences(EncounterType[] old, EncounterType[] combo)
        {
            int diff = 0;
            for (int i = 0; i < old.Length && i < combo.Length; i++)
                if (old[i] != combo[i]) diff++;
            return diff;
        }

        /// <summary>
        /// Returner true hvis combo indeholder mindst en node af typen t.
        /// Simpelt lineært scan; brugt som filter i TryFixRow når desiredType er sat.
        /// </summary>
        private bool ContainsType(EncounterType[] combo, EncounterType t)
        {
            for (int i = 0; i < combo.Length; i++)
                if (combo[i] == t) return true;
            return false;
        }

        /// <summary>
        /// Til shuffle af en liste ved brug af givet RNG.
        /// - Generisk helper der understøtter alle liste-typer.
        /// Kompleksitet: O(n).
        /// </summary>
        private void Shuffle<T>(List<T> list, System.Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        /// <summary>
        /// Debug-udskrift der viser per-række: antal noder, budget, aktuel cost, og liste af typer.
        /// - Ignorerer række 0 og sidste række (start/boss).
        /// - Anvender GetCost til at beregne total cost.
        /// - Egnet til konsol-logging i Unity for at forstå nuværende state under generation.
        /// </summary>
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
