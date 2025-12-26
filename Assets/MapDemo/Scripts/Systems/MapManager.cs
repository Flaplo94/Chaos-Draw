using MapDemo.Settings;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Diagnostics;

public class MapManager : MonoBehaviour
{
    // --------------------------
    // UI referencer (tilknyt i Inspector)
    // --------------------------
    [Header("UI References")]
    [SerializeField] private RectTransform mapArea;    // Området over pergamentet hvor kortet ritas
    [SerializeField] private RectTransform nodesParent; // Forælder for node-UI elementer
    [SerializeField] private RectTransform edgesParent; // Forælder for kant-UI elementer
    [SerializeField] private GameObject nodePrefab;    // Prefab: UI Button/Image (RectTransform + Button + Image)
    [SerializeField] private GameObject edgePrefab;    // Prefab med Image (root eller child)
    [SerializeField] private MapBottomInfo bottomInfo; // UI komponent som viser statistik og status
    [SerializeField] private bool labelsEnabled = false;
    [SerializeField] private bool dimmingEnabled = true;

    // --------------------------
    // Layout parametre
    // --------------------------
    [Header("Layout")]
    [SerializeField, Min(3)] private int totalRows = 15; // Antal rækker i kortet (inkl. start + boss)
    [SerializeField] private int minNodesPerRow = 2;
    [SerializeField] private int maxNodesPerRow = 4;

    [SerializeField] private float topPadding = 120f;
    [SerializeField] private float bottomPadding = 120f;
    [SerializeField] private float leftPadding = 120f;
    [SerializeField] private float rightPadding = 120f;
    [SerializeField] private float nodeMinHorizontalGap = 80f;

    // --------------------------
    // Kantegenskaber
    // --------------------------
    [Header("Edges")]
    [SerializeField] private float edgeThickness = 6f;
    [SerializeField, Range(0f, 1f)] private float edgeAlpha = 0.85f;
    [SerializeField] private Color travelledEdgeColor = Color.yellow; // Farve for allerede valgte/tilbagelagte kanter
    [SerializeField] private float unusedEdgeAlpha = 0.2f; // Dæmpnings-alpha for ikke-aktuelle kanter

    // --------------------------
    // Random / seed til reproducerbarhed
    // --------------------------
    [Header("Random")]
    [SerializeField] private bool useFixedSeed = false;
    [SerializeField] private int seed = 123456;

    // Hjælpefunktion: beregn horisontal midt (bruger paddings)
    private float CenterX()
    {
        float minX = -mapArea.rect.width * 0.5f + leftPadding;
        float maxX = mapArea.rect.width * 0.5f - rightPadding;
        return (minX + maxX) * 0.5f;
    }

    [Header("Map Style")]
    [SerializeField] private MapSettings mapSettings; // Stil/skin data til noder og kanter


    // --------------------------
    // Data / runtime state
    // --------------------------
    public MapGraph Graph { get; private set; } = new MapGraph(); // Graph model: noder + kanter

    // Traversal state
    private MapNodeData currentNode; // Den node spilleren sidst valgte (null før første valg)
    private readonly Dictionary<int, MapNodeButton> nodeButtons = new(); // id -> UI komponent for node
    private readonly Dictionary<(int fromId, int toId), Image> edgeImages = new(); // kant -> Image reference (til opdatering)

    // Visuelle instanser (bruges ved rydning)
    private readonly List<GameObject> spawnedNodes = new();
    private readonly List<Image> spawnedEdges = new();

    // --------------------------
    // Kørsel / timing statistik (til BottomInfo og målinger)
    // --------------------------
    private bool runActive;
    private bool runCompleted;
    private float runStartTime;          // Time.time ved Regenerate
    private float lastClickTime;         // Tidspunkt for sidste gyldige klik
    private readonly List<float> clickIntervals = new(); // Liste af interval-tider mellem valg
    private double lastGenerationMs;       // Måling af hvor lang tid generering tog (ms)

    // --------------------------
    // Encounter-generatorer (flere algoritmer støttes)
    // --------------------------
    private readonly EncounterGeneratorOptionA optionAGenerator = new EncounterGeneratorOptionA();
    private bool optionAPassLastRun;

    private readonly EncounterGeneratorOptionB optionBGenerator = new EncounterGeneratorOptionB();
    private bool optionBPassLastRun;
    private string optionBFailReason;

    [SerializeField] private OptionCSettings optionCSettings;
    private readonly EncounterGeneratorOptionC optionCGenerator = new EncounterGeneratorOptionC();
    private bool optionCPassLastRun;

    public enum EncounterGenerationMode
    {
        OptionA = 0,
        OptionB = 1,
        OptionC = 2,
        OptionD = 3
    }
    [SerializeField] private EncounterGenerationMode encounterMode = EncounterGenerationMode.OptionA;


    // ------------------------------------------------------------
    // PUBLIC API
    // ------------------------------------------------------------

    /// <summary>
    /// Regenerer hele kortet. Kaldt fra UI (Regenerate-knap).
    /// Opbygger Graph, tildeler encounters, bygger visuals og sætter start-tilstand.
    /// </summary>
    public void RegenerateMap()
    {
        var stopwatch = Stopwatch.StartNew();
        var rng = useFixedSeed ? new System.Random(seed) : new System.Random();

        // Start ny run-timing
        ResetRunTiming();

        // Fjern tidligere runtime-objekter og state
        ClearAllRuntime();

        // Generer graf / encounters / visuals
        GenerateGraph(rng);
        AssignEncounters(rng);
        BuildEdges();
        BuildNodes();
        SetupStartNode();

        // Auto-klik på start hvis tilgængelig (vælg første node)
        if (Graph.rows.Count > 0 && Graph.rows[0].Count > 0)
        {
            var start = Graph.rows[0][0];
            OnNodeClicked(start.id);
        }

        stopwatch.Stop();
        lastGenerationMs = stopwatch.Elapsed.TotalMilliseconds;
        UpdateBottomInfo();
        UnityEngine.Debug.Log($"Regenerate seed={seed} useFixedSeed={useFixedSeed}");
    }

    /// <summary>
    /// Reset path: behold layout, men nulstil traversal (starter forfra).
    /// Bruges når spilleren vil genstarte samme kort.
    /// </summary>
    public void ResetPath()
    {
        // Deaktiver interaktion på alle node-knapper
        foreach (var kvp in nodeButtons)
        {
            var btn = kvp.Value;
            if (btn == null) continue;
            btn.SetInteractable(false);
        }

        // Reset kant-visualer til default
        foreach (var kvp in edgeImages)
        {
            var img = kvp.Value;
            if (img == null) continue;
            var c = Color.white;
            c.a = edgeAlpha;
            img.color = c;
        }

        currentNode = null;
        SetupStartNode();
        ResetRunTiming();
        UpdateBottomInfo();
    }

    // ------------------------------------------------------------
    // INTERNAL: CLEANUP / GENERATION
    // ------------------------------------------------------------

    /// <summary>
    /// Fjern alle runtime-instansierede noder/kanter og ryd intern state.
    /// Kaldes før ny generering.
    /// </summary>
    private void ClearAllRuntime()
    {
        // Fjern visuelle children fra parents
        for (int i = nodesParent.childCount - 1; i >= 0; i--)
            Destroy(nodesParent.GetChild(i).gameObject);
        for (int i = edgesParent.childCount - 1; i >= 0; i--)
            Destroy(edgesParent.GetChild(i).gameObject);

        spawnedNodes.Clear();
        spawnedEdges.Clear();

        // Ryd data og state
        Graph.Clear();
        nodeButtons.Clear();
        edgeImages.Clear();
        currentNode = null;
    }

    /// <summary>
    /// Genererer grafens struktur: antal rækker, noder pr række og forbindelser (planar, ikke-overlappende).
    /// </summary>
    private void GenerateGraph(System.Random rng)
    {
        Graph.totalRows = totalRows;

        // Opret noder pr række. Række 0 og sidste række er special (start + boss).
        for (int r = 0; r < totalRows; r++)
        {
            float y = RowY(r);

            if (r == 0 || r == totalRows - 1)
            {
                // START + END: altid 1 node, centreret horisontalt
                float xCenter = CenterX();
                Graph.AddNode(r, new Vector2(xCenter, y));
            }
            else
            {
                // Midterrækker: random antal noder og placering
                int count = rng.Next(minNodesPerRow, maxNodesPerRow + 1);
                var xs = SampleDistinctX(count, nodeMinHorizontalGap, rng);

                for (int i = 0; i < count; i++)
                    Graph.AddNode(r, new Vector2(xs[i], y));

                Graph.SortRowByX(r);
            }
        }

        // Forbind hver tilstødende række med planerede, ikke-krydsende kanter
        for (int r = 0; r < totalRows - 1; r++)
            ConnectRowsPlanar(Graph.rows[r], Graph.rows[r + 1], rng);
    }


    // ------------------------------------------------------------
    // CONNECT ROWS (non-crossing)
    // ------------------------------------------------------------

    /// <summary>
    /// Forbinder to rækker med kanter uden kryds (bevarer planarity).
    /// Algoritmen sikrer minimumsindkommende og udgående grad mv.
    /// </summary>
    private void ConnectRowsPlanar(List<MapNodeData> fromRow, List<MapNodeData> toRow, System.Random rng)
    {
        int nA = fromRow.Count;
        int nB = toRow.Count;
        if (nA == 0 || nB == 0)
            return;

        // Primær monotone mapping (ingen kryds)
        for (int i = 0; i < nA; i++)
        {
            float t = (nA == 1) ? 0f : (float)i / (nA - 1);
            int j = Mathf.RoundToInt(t * (nB - 1));
            AddEdgeIfValid(fromRow[i], toRow[j]);
        }

        // Sikre at hver node i næste række har mindst én indkommende kant
        for (int j = 0; j < nB; j++)
        {
            var to = toRow[j];
            if (!HasIncomingFromPrevRow(to, fromRow))
            {
                float t = (nB == 1) ? 0f : (float)j / (nB - 1);
                int i = Mathf.RoundToInt(t * (nA - 1));
                AddEdgeIfValid(fromRow[i], to);
            }
        }

        // Valgfri: tilføj ekstra naboer (stadig uden kryds)
        for (int i = 0; i < nA; i++)
        {
            var from = fromRow[i];
            float t = (nA == 1) ? 0f : (float)i / (nA - 1);
            int center = Mathf.RoundToInt(t * (nB - 1));

            TryAddNeighbor(from, toRow, center - 1, rng);
            TryAddNeighbor(from, toRow, center + 1, rng);
        }
    }

    /// <summary>
    /// Forsøg at tilføje en nabo (tilstødende indeks) baseret på tilfældighed.
    /// </summary>
    private void TryAddNeighbor(MapNodeData from, List<MapNodeData> toRow, int j, System.Random rng)
    {
        if (j < 0 || j >= toRow.Count) return;

        // 50% chance for at tilføje
        if (rng.NextDouble() > 0.5) return;

        AddEdgeIfValid(from, toRow[j]);
    }

    /// <summary>
    /// Tilføj en kant mellem from -> to hvis den opfylder regler:
    /// - kun til næste række
    /// - max 2 ind/ud for de fleste noder
    /// - ingen kryds
    /// - start/boss håndteres som special cases
    /// </summary>
    private void AddEdgeIfValid(MapNodeData from, MapNodeData to)
    {
        if (from == null || to == null) return;
        if (to.rowIndex != from.rowIndex + 1) return;

        // Undgå duplikater
        if (from.outgoing.Contains(to.id))
            return;

        int lastRow = Graph.totalRows - 1;

        // SPECIAL: Start node forbinder til alle i row1
        if (from.rowIndex == 0)
        {
            Graph.AddEdge(from, to);
            return;
        }

        // SPECIAL: Alle i forrige række før boss forbinder til bossen
        if (to.rowIndex == lastRow)
        {
            Graph.AddEdge(from, to);
            return;
        }

        // Max 2 outgoing fra en node
        if (GetOutgoingCount(from) >= 2)
            return;

        // Max 2 incoming til en node
        if (GetIncomingCount(to) >= 2)
            return;

        // Krydsningskontrol
        if (WouldCreateCrossing(from, to))
            return;

        Graph.AddEdge(from, to);
    }

    /// <summary>
    /// Check om 'to' allerede har indkommende fra en node i fromRow.
    /// </summary>
    private bool HasIncomingFromPrevRow(MapNodeData to, List<MapNodeData> fromRow)
    {
        foreach (var from in fromRow)
            for (int k = 0; k < from.outgoing.Count; k++)
                if (from.outgoing[k] == to.id)
                    return true;
        return false;
    }

    /// <summary>
    /// Returnerer true hvis en foreslået kant vil skabe en krydsning med en eksisterende kant.
    /// </summary>
    private bool WouldCreateCrossing(MapNodeData newFrom, MapNodeData newTo)
    {
        int rowA = newFrom.rowIndex;
        int rowB = newTo.rowIndex; // should be rowA+1

        foreach (var edge in Graph.edges)
        {
            var a = Graph.GetNodeById(edge.fromNodeId);
            var b = Graph.GetNodeById(edge.toNodeId);
            if (a == null || b == null) continue;
            if (a.rowIndex != rowA || b.rowIndex != rowB) continue;

            bool fromLeft = newFrom.colIndex < a.colIndex;
            bool toRight = newTo.colIndex > b.colIndex;
            bool fromRight = newFrom.colIndex > a.colIndex;
            bool toLeft = newTo.colIndex < b.colIndex;

            if ((fromLeft && toRight) || (fromRight && toLeft))
                return true;
        }

        return false;
    }

    // ------------------------------------------------------------
    // BUILD VISUALS
    // ------------------------------------------------------------

    /// <summary>
    /// Instantiate node-prefabs for hver node i Graph og sæt opførsel / styling.
    /// </summary>
    private void BuildNodes()
    {
        nodeButtons.Clear();
        currentNode = null;

        for (int r = 0; r < Graph.rows.Count; r++)
        {
            foreach (var node in Graph.rows[r])
            {
                var go = Instantiate(nodePrefab, nodesParent);
                spawnedNodes.Add(go);

                var rt = go.GetComponent<RectTransform>();
                if (rt == null)
                {
                    UnityEngine.Debug.LogError("NodePrefab root needs a RectTransform.");
                    continue;
                }

                // Placér UI elementet ved den beregnede anker position
                rt.anchoredPosition = node.anchoredPos;

                var nodeBtn = go.GetComponent<MapNodeButton>();
                if (nodeBtn == null)
                {
                    UnityEngine.Debug.LogError("NodePrefab is missing MapNodeButton on the root.");
                    continue;
                }

                if (nodeBtn.button == null || nodeBtn.baseImage == null)
                {
                    UnityEngine.Debug.LogError("MapNodeButton: assign Button + BaseImage (and Ring/Icon) in the prefab.");
                    continue;
                }

                // Bestem visuel type ud fra encounterType (evt. override for start/boss)
                EncounterType visualType = node.encounterType;

                if (r == 0) visualType = EncounterType.Start;
                else if (r == Graph.totalRows - 1) visualType = EncounterType.Boss;

                // Anvend stil fra MapSettings hvis tilgængelig
                if (mapSettings != null)
                    nodeBtn.ApplyStyle(mapSettings, visualType);

                // Sæt label fra MapSettings style (hvis aktiveret)
                if (mapSettings != null && nodeBtn.labelText != null)
                {
                    string label = null;

                    if (mapSettings.TryGetStyle(visualType, out var style) && !string.IsNullOrEmpty(style.label))
                        label = style.label;
                    else
                        label = visualType.ToString();

                    nodeBtn.SetLabel(label);
                    nodeBtn.SetLabelVisible(labelsEnabled);
                }

                // Registrer knap og klik-adfærd
                nodeBtn.nodeId = node.id;
                nodeButtons[node.id] = nodeBtn;

                nodeBtn.button.onClick.RemoveAllListeners();
                int capturedId = node.id;
                nodeBtn.button.onClick.AddListener(() => OnNodeClicked(capturedId));

                nodeBtn.SetInteractable(false);
            }
        }
    }


    /// <summary>
    /// Byg kant-visualer (UI Images) for hver kant i Graph.
    /// Håndterer rotation, længde og alpha.
    /// </summary>
    private void BuildEdges()
    {
        edgeImages.Clear();

        foreach (var edge in Graph.edges)
        {
            var from = Graph.GetNodeById(edge.fromNodeId);
            var to = Graph.GetNodeById(edge.toNodeId);
            if (from == null || to == null) continue;

            var go = Instantiate(edgePrefab, edgesParent);
            var img = go.GetComponent<Image>() ?? go.GetComponentInChildren<Image>();
            if (img == null)
            {
                UnityEngine.Debug.LogError("Edge prefab must have an Image.");
                Destroy(go);
                continue;
            }

            if (mapSettings != null && img != null)
            {
                if (mapSettings.edgeLineSprite != null)
                    img.sprite = mapSettings.edgeLineSprite;

                img.color = Color.white; // Basisfarve, alpha justeres nedenfor
            }

            img.raycastTarget = false;

            // Positioner og roter linjen mellem nodernes anker-positioner
            var rt = img.rectTransform;
            Vector2 a = from.anchoredPos;
            Vector2 b = to.anchoredPos;
            Vector2 dir = b - a;
            float len = dir.magnitude;
            Vector2 mid = a + dir * 0.5f;

            rt.anchoredPosition = mid;
            rt.sizeDelta = new Vector2(len, edgeThickness);
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            rt.localRotation = Quaternion.Euler(0, 0, angle);

            var c = Color.white;
            c.a = edgeAlpha;
            img.color = c;

            spawnedEdges.Add(img);
            edgeImages[(edge.fromNodeId, edge.toNodeId)] = img;
        }
    }

    /// <summary>
    /// Forbered start-tilstand: marker første node som klikbar og highlight reachable edges.
    /// </summary>
    private void SetupStartNode()
    {
        if (Graph.rows.Count == 0 || Graph.rows[0].Count == 0)
            return;

        var start = Graph.rows[0][0];
        HighlightReachableEdges(Graph.rows[0][0]);
        // kun denne node er klikbar i starten
        if (nodeButtons.TryGetValue(start.id, out var btn))
            btn.SetInteractable(true);

        currentNode = null; // først valgt når brugeren klikker
    }

    // ------------------------------------------------------------
    // TRAVERSAL LOGIK
    // ------------------------------------------------------------

    /// <summary>
    /// Hovedmetode ved node-klik. Håndterer både første klik (start) og efterfølgende bevægelser langs kanter.
    /// Validerer forbindelser, opdaterer visuelle markeringer og statistik.
    /// </summary>
    private void OnNodeClicked(int nodeId)
    {
        if (!nodeButtons.TryGetValue(nodeId, out var clickedBtn))
            return;

        var clicked = Graph.GetNodeById(nodeId);
        if (clicked == null) return;

        // Første klik (vælg start)
        if (currentNode == null)
        {
            if (clicked.rowIndex != 0)
            {
                UnityEngine.Debug.Log("You must start at the top node.");
                return;
            }

            if (runActive)
            {
                float now = Time.time;
                float dt = now - lastClickTime;
                if (dt < 0f) dt = 0f;
                clickIntervals.Add(dt);      // Regenerate -> first node
                lastClickTime = now;
            }

            currentNode = clicked;
            clickedBtn.SetSelected();
            DisableAllNodes();
            EnableNextRow(clicked);
            HighlightReachableEdges(clicked);

            UpdateBottomInfo(); // Opdater bottom-info UI
            return;
        }

        // Tjek at den valgte node er forbundet fra currentNode
        bool isConnected = false;
        foreach (var edge in Graph.edges)
        {
            if (edge.fromNodeId == currentNode.id && edge.toNodeId == clicked.id)
            {
                isConnected = true;

                // Highlight den gennemgåede kant (markér som travellled)
                if (edgeImages.TryGetValue((edge.fromNodeId, edge.toNodeId), out var img))
                {
                    var c = travelledEdgeColor;
                    c.a = 1f;
                    img.color = c;
                }
                break;
            }
        }

        if (!isConnected)
        {
            UnityEngine.Debug.Log("That node is not connected to your current node.");
            return;
        }

        // Optag tid siden sidste gyldige valg (til statistik)
        if (runActive)
        {
            float now = Time.time;
            float dt = now - lastClickTime;
            if (dt < 0f) dt = 0f;
            clickIntervals.Add(dt);
            lastClickTime = now;
        }

        // Fade ubrugte outgoing edges fra forrige node
        foreach (var edge in Graph.edges)
        {
            if (edge.fromNodeId == currentNode.id && edge.toNodeId != clicked.id)
            {
                if (edgeImages.TryGetValue((edge.fromNodeId, edge.toNodeId), out var img))
                {
                    var c = img.color;
                    c.a = unusedEdgeAlpha;
                    img.color = c;
                }
            }
        }

        // Flyt markøren fremad
        currentNode = clicked;
        clickedBtn.SetSelected();
        DisableAllNodes();
        EnableNextRow(clicked);
        HighlightReachableEdges(clicked);
        UpdateBottomInfo();

        // Hvis nået sidste række (boss): afslut run
        if (clicked.rowIndex == Graph.totalRows - 1)
        {
            runActive = false;
            runCompleted = true;

            // skriv statistik til BottomInfo
            UpdateBottomInfo();
            UnityEngine.Debug.Log("Reached final node.");
            DisableAllNodes();
        }

        // Dobbelt-check: lock alt ved sidste række
        if (clicked.rowIndex == Graph.totalRows - 1)
        {
            UnityEngine.Debug.Log("Reached final node.");
            DisableAllNodes();
        }
    }

    // Deaktiver alle noder (gør dem ikke-interaktive)
    private void DisableAllNodes()
    {
        foreach (var kvp in nodeButtons)
            kvp.Value.SetInteractable(false);
    }

    // Aktiver kun noder i næste række som er forbundet til den aktuelle node
    private void EnableNextRow(MapNodeData node)
    {
        foreach (var edge in Graph.edges)
        {
            if (edge.fromNodeId == node.id)
            {
                if (nodeButtons.TryGetValue(edge.toNodeId, out var btn))
                    btn.SetInteractable(true);
            }
        }
    }

    // ------------------------------------------------------------
    // LAYOUT HELPERS
    // ------------------------------------------------------------

    /// <summary>
    /// Beregn Y position for række r (top=0, bottom=last).
    /// </summary>
    private float RowY(int r)
    {
        float h = mapArea.rect.height;
        float usable = h - topPadding - bottomPadding;
        float t = (totalRows <= 1) ? 0f : (float)r / (totalRows - 1); // 0..1

        // anchoredPosition: top = +h/2, bottom = -h/2
        return (h * 0.5f - topPadding) - t * usable;
    }

    /// <summary>
    /// Prøver at sample unikke X-koordinater inden for minX..maxX med minGap.
    /// Hvis sampling fejler, fallback til jævnt fordelte værdier.
    /// </summary>
    private List<float> SampleDistinctX(int count, float minGap, System.Random rng)
    {
        float minX = -mapArea.rect.width * 0.5f + leftPadding;
        float maxX = mapArea.rect.width * 0.5f - rightPadding;

        var xs = new List<float>(count);
        int safety = 0;

        while (xs.Count < count && safety < 2000)
        {
            safety++;
            float x = NextFloat(rng, minX, maxX);

            bool ok = true;
            for (int i = 0; i < xs.Count; i++)
                if (Mathf.Abs(xs[i] - x) < minGap) { ok = false; break; }

            if (ok) xs.Add(x);
        }

        if (xs.Count < count)
        {
            // fallback: jævn fordeling hvis sampling mislykkes
            xs.Clear();
            float step = (maxX - minX) / (count + 1);
            for (int i = 1; i <= count; i++)
                xs.Add(minX + step * i);
        }

        return xs;
    }

    /// <summary>
    /// Opdater kant-visualer: dim eller fremhæv baseret på om de er reachable eller allerede travelled.
    /// Dimming kan slås fra for at vise alle kanter lige meget.
    /// </summary>
    private void HighlightReachableEdges(MapNodeData fromNode)
    {
        if (fromNode == null) return;

        // Hvis dimming slået fra: sæt alle ikke-travelled edges til normal alpha
        if (!dimmingEnabled)
        {
            foreach (var kvp in edgeImages)
            {
                var img = kvp.Value;
                if (img == null) continue;

                // Hvis kant er travelled, behold dens farve
                if (img.color == travelledEdgeColor)
                    continue;

                var c = img.color;
                c.a = edgeAlpha;
                img.color = c;
            }

            return;
        }

        // Dimming er PÅ: først dim alle ikke-travelled
        foreach (var kvp in edgeImages)
        {
            var img = kvp.Value;
            if (img == null) continue;

            if (img.color == travelledEdgeColor)
            {
                // keep travelled edges fully visible
                var c = img.color;
                c.a = 1f;
                img.color = c;
                continue;
            }

            var dim = img.color;
            dim.a = unusedEdgeAlpha;
            img.color = dim;
        }

        // Så fremhæv reachable edges fra given node
        foreach (var edge in Graph.edges)
        {
            if (edge.fromNodeId == fromNode.id)
            {
                if (edgeImages.TryGetValue((edge.fromNodeId, edge.toNodeId), out var img))
                {
                    // Hvis allerede travelled, bevar travelled-farve
                    if (img.color == travelledEdgeColor)
                    {
                        var c = img.color;
                        c.a = 1f;
                        img.color = c;
                    }
                    else
                    {
                        var c = img.color;
                        c.a = 1f;
                        img.color = c;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Sammensæt og vis data i bunden af UI: tid, encounter counts, statistikker osv.
    /// Samler også run-statistikker (gennemsnit, median).
    /// </summary>
    private void UpdateBottomInfo()
    {
        if (bottomInfo == null)
            return;

        // Tæl encounters og one-choice nodes
        var encounterCounts = new Dictionary<string, int>();
        int oneChoiceNodes = 0;

        for (int r = 0; r < Graph.rows.Count; r++)
        {
            foreach (var node in Graph.rows[r])
            {
                string key = node.encounterType.ToString();
                if (!encounterCounts.ContainsKey(key))
                    encounterCounts[key] = 0;
                encounterCounts[key]++;

                if (node.outgoing != null && node.outgoing.Count == 1)
                    oneChoiceNodes++;
            }
        }

        // Sidste interval i sekunder
        float lastInterval = 0f;
        if (clickIntervals.Count > 0)
            lastInterval = clickIntervals[clickIntervals.Count - 1];

        // Totaler kun hvis run er fuldendt med mindst ét step
        float totalRunSeconds = 0f;
        float avgInterval = 0f;
        float medianInterval = 0f;
        bool hasIntervals = clickIntervals.Count > 0;

        if (runCompleted && hasIntervals)
        {
            foreach (var dt in clickIntervals)
                totalRunSeconds += dt;

            avgInterval = totalRunSeconds / clickIntervals.Count;

            var sorted = new List<float>(clickIntervals);
            sorted.Sort();
            int n = sorted.Count;
            if (n % 2 == 1)
                medianInterval = sorted[n / 2];
            else
                medianInterval = (sorted[n / 2 - 1] + sorted[n / 2]) * 0.5f;
        }

        bottomInfo.ShowInfo(
            lastGenerationMs,
            oneChoiceNodes,
            encounterCounts,
            runCompleted && hasIntervals,
            lastInterval,
            totalRunSeconds,
            avgInterval,
            medianInterval,
            optionAPassLastRun,
            optionCPassLastRun,
            optionBPassLastRun,
            encounterMode
        );
    }

    // --------------------------
    // Konfigurationshjælpere (seed etc.)
    // --------------------------
    public void SetSeed(int newSeed)
    {
        useFixedSeed = true;
        seed = newSeed;
    }

    public void UseRandomSeed()
    {
        useFixedSeed = false;
        // Optional: seed = Random.Range(int.MinValue, int.MaxValue);
    }

    public void SetLabelsEnabled(bool enabled)
    {
        labelsEnabled = enabled;

        foreach (var kvp in nodeButtons)
        {
            kvp.Value.SetLabelVisible(labelsEnabled);
        }
    }

    public void SetDimmingEnabled(bool enabled)
    {
        dimmingEnabled = enabled;

        // Re-apply highlight state efter ændring
        if (currentNode != null)
            HighlightReachableEdges(currentNode);
        else if (Graph.rows.Count > 0 && Graph.rows[0].Count > 0)
            HighlightReachableEdges(Graph.rows[0][0]);
    }

    /// <summary>
    /// Nulstil run-timing (til statistik ved ny run).
    /// </summary>
    private void ResetRunTiming()
    {
        runActive = true;
        runCompleted = false;
        runStartTime = Time.time;
        lastClickTime = runStartTime;
        clickIntervals.Clear();
    }

    private int GetOutgoingCount(MapNodeData from)
    {
        // Returner antal udgående (bruges ved validering)
        return from.outgoing != null ? from.outgoing.Count : 0;
    }

    private int GetIncomingCount(MapNodeData to)
    {
        int count = 0;
        foreach (var edge in Graph.edges)
        {
            if (edge.toNodeId == to.id)
                count++;
        }
        return count;
    }

    /// <summary>
    /// Tildel encounter-typer til noder i Graph vha. valgt mode (OptionA/B/C).
    /// </summary>
    private void AssignEncounters(System.Random rng)
    {
        if (Graph == null || Graph.rows == null) return;

        optionAPassLastRun = false;
        optionCPassLastRun = false;

        switch (encounterMode)
        {
            case EncounterGenerationMode.OptionA:
                {
                    optionAGenerator.Generate(Graph, rng, out optionAPassLastRun);
                    break;
                }

            case EncounterGenerationMode.OptionB:
                {
                    optionBGenerator.Generate(Graph, seed, out optionBPassLastRun, out optionBFailReason);

                    if (!optionBPassLastRun && !string.IsNullOrEmpty(optionBFailReason))
                        UnityEngine.Debug.LogWarning("[OptionB FAIL] " + optionBFailReason);

                    break;
                }


            case EncounterGenerationMode.OptionC:
                {
                    optionCGenerator.Generate(Graph, rng, optionCSettings, out optionCPassLastRun);
                    UnityEngine.Debug.Log($"[OptionC] Using settings instance: {optionCSettings.GetHashCode()}  costElite={optionCSettings.costElite}  row1Budget={optionCSettings.rowBudgets[1]}  mode={encounterMode}");
                    DumpAllRowsOptionCDebug();

                    var problems = new List<string>();
                    optionCPassLastRun = ValidateOptionCAndLog(problems);

                    if (!optionCPassLastRun)
                    {
                        // Print the first few fejl så man hurtigt kan debugge
                        for (int i = 0; i < Mathf.Min(5, problems.Count); i++)
                            UnityEngine.Debug.LogError("[Option C FAIL] " + problems[i]);
                    }
                    break;
                }
        }
    }

    public void SetEncounterMode(int index)
    {
        encounterMode = (EncounterGenerationMode)index;
    }

    /// <summary>
    /// Valider Option C's per-row budget constraints og log problemer.
    /// </summary>
    private bool ValidateOptionCAndLog(List<string> problems)
    {
        problems.Clear();

        if (Graph == null || Graph.rows == null || optionCSettings == null)
            return false;

        int lastRow = Graph.totalRows - 1;

        for (int r = 1; r < lastRow; r++)
        {
            var row = Graph.rows[r];
            if (row == null || row.Count == 0) continue;

            int budget = optionCSettings.rowBudgets[r];
            int minAllowed = budget - 1;
            int maxAllowed = budget + 1;

            int cost = 0;
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < row.Count; i++)
            {
                var t = row[i].encounterType;
                cost += GetCostOptionC(t);
                sb.Append(t);
                if (i < row.Count - 1) sb.Append(", ");
            }

            if (cost < minAllowed || cost > maxAllowed)
            {
                problems.Add($"Row {r}: budget={budget} allowed[{minAllowed},{maxAllowed}] cost={cost} types=[{sb}]");
            }
        }

        return problems.Count == 0;
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void DumpAllRowsOptionCDebug()
    {
        if (Graph == null || Graph.rows == null || optionCSettings == null)
        {
            UnityEngine.Debug.LogWarning("[OptionC] Dump skipped (Graph/rows/settings missing).");
            return;
        }

        int lastRow = Graph.totalRows - 1;

        for (int r = 1; r < lastRow; r++)
        {
            var row = Graph.rows[r];
            if (row == null || row.Count == 0) continue;

            int budget = optionCSettings.rowBudgets[r];
            int minAllowed = budget - 1;
            int maxAllowed = budget + 1;

            int cost = 0;
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < row.Count; i++)
            {
                var t = row[i].encounterType;
                cost += GetCostOptionC(t);

                sb.Append(t);
                if (i < row.Count - 1) sb.Append(", ");
            }

            UnityEngine.Debug.Log($"[OptionC] Row {r}: nodes={row.Count} budget={budget} cost={cost} types=[{sb}] allowed=[{minAllowed},{maxAllowed}]");
        }
    }
    // Hjælpefunktion: cost mapping for OptionC
    private int GetCostOptionC(EncounterType type)
    {
        switch (type)
        {
            case EncounterType.Elite: return optionCSettings.costElite;
            case EncounterType.Special: return optionCSettings.costSpecial;
            case EncounterType.Event: return optionCSettings.costEvent;
            case EncounterType.Shop: return optionCSettings.costShop;
            default: return optionCSettings.costNormal;
        }
    }
    /// <summary>
    /// Simpel tilfældig float generator i [min,max) baseret på System.Random.
    /// </summary>
    private float NextFloat(System.Random rng, float min, float max)
    {
        return (float)(min + (max - min) * rng.NextDouble());
    }

    public void SetFixedSeed(int s)
    {
        seed = s;
        useFixedSeed = true;
    }

    public void DisableFixedSeed()
    {
        useFixedSeed = false;
    }
    public int GetSeed() => seed;
    public bool IsFixedSeed() => useFixedSeed;
}
