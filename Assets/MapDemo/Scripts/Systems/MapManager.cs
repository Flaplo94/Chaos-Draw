using MapDemo.Settings;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Diagnostics;

public class MapManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RectTransform mapArea;    // Area over the parchment
    [SerializeField] private RectTransform nodesParent;
    [SerializeField] private RectTransform edgesParent;
    [SerializeField] private GameObject nodePrefab;    // UI Button/Image (RectTransform + Button + Image)
    [SerializeField] private GameObject edgePrefab;    // Prefab with an Image (root or child)
    [SerializeField] private MapBottomInfo bottomInfo;
    [SerializeField] private bool labelsEnabled = false;
    [SerializeField] private bool dimmingEnabled = true;

    [Header("Layout")]
    [SerializeField, Min(3)] private int totalRows = 15;
    [SerializeField] private int minNodesPerRow = 2;
    [SerializeField] private int maxNodesPerRow = 4;

    [SerializeField] private float topPadding = 120f;
    [SerializeField] private float bottomPadding = 120f;
    [SerializeField] private float leftPadding = 120f;
    [SerializeField] private float rightPadding = 120f;
    [SerializeField] private float nodeMinHorizontalGap = 80f;

    [Header("Edges")]
    [SerializeField] private float edgeThickness = 6f;
    [SerializeField, Range(0f, 1f)] private float edgeAlpha = 0.85f;
    [SerializeField] private Color travelledEdgeColor = Color.yellow;
    [SerializeField] private float unusedEdgeAlpha = 0.2f;

    [Header("Random")]
    [SerializeField] private bool useFixedSeed = false;
    [SerializeField] private int seed = 123456;

    private float CenterX()
    {
        float minX = -mapArea.rect.width * 0.5f + leftPadding;
        float maxX = mapArea.rect.width * 0.5f - rightPadding;
        return (minX + maxX) * 0.5f;
    }

    [Header("Map Style")]
    [SerializeField] private MapSettings mapSettings;


    // Data
    public MapGraph Graph { get; private set; } = new MapGraph();

    // State for traversal
    private MapNodeData currentNode;
    private readonly Dictionary<int, MapNodeButton> nodeButtons = new();
    private readonly Dictionary<(int fromId, int toId), Image> edgeImages = new();

    // Visual references (for cleanup)
    private readonly List<GameObject> spawnedNodes = new();
    private readonly List<Image> spawnedEdges = new();

    // --- Run timing stats ---
    private bool runActive;
    private bool runCompleted;
    private float runStartTime;          // Time.time when Regenerate was pressed
    private float lastClickTime;         // last successful node selection time
    private readonly List<float> clickIntervals = new(); // seconds between selections
    private double lastGenerationMs;       // for display

    // Option A generator (normal C# helper)
    private readonly EncounterGeneratorOptionA optionAGenerator = new EncounterGeneratorOptionA();

    // For the “A lamp” in BottomInfo
    private bool optionAPassLastRun;

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

    // Called by Regenerate button
    public void RegenerateMap()
    {
        var stopwatch = Stopwatch.StartNew();

        // start a fresh run timing from this regenerate click
        ResetRunTiming();

        ClearAllRuntime();
        GenerateGraph();
        AssignEncounters();
        BuildEdges();
        BuildNodes();
        SetupStartNode();
        if (Graph.rows.Count > 0 && Graph.rows[0].Count > 0)
        {
            var start = Graph.rows[0][0];
            OnNodeClicked(start.id);
        }

        stopwatch.Stop();
        lastGenerationMs = stopwatch.Elapsed.TotalMilliseconds;
         // if you still want an int field
        //UnityEngine.Debug.Log($"Map gen: {ms:0.000} ms");
        UpdateBottomInfo();
    }

    // Called by Reset button (same layout, restart path)
    public void ResetPath()
    {
        // Reset node visuals & interactivity
        foreach (var kvp in nodeButtons)
        {
            var btn = kvp.Value;
            if (btn == null) continue;
            btn.SetInteractable(false);
        }

        // Reset edges to default visual
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

    private void ClearAllRuntime()
    {
        // Destroy visuals
        for (int i = nodesParent.childCount - 1; i >= 0; i--)
            Destroy(nodesParent.GetChild(i).gameObject);
        for (int i = edgesParent.childCount - 1; i >= 0; i--)
            Destroy(edgesParent.GetChild(i).gameObject);

        spawnedNodes.Clear();
        spawnedEdges.Clear();

        // Clear data/state
        Graph.Clear();
        nodeButtons.Clear();
        edgeImages.Clear();
        currentNode = null;
    }

    private void GenerateGraph()
    {
        if (useFixedSeed)
            Random.InitState(seed);

        Graph.totalRows = totalRows;

        // --- Create nodes per row (top = 0, bottom = last) ---
        for (int r = 0; r < totalRows; r++)
        {
            float y = RowY(r);

            if (r == 0 || r == totalRows - 1)
            {
                // START + END: always 1 node, centered horizontally
                float xCenter = CenterX();
                Graph.AddNode(r, new Vector2(xCenter, y));
            }
            else
            {
                // Middle rows: 2–4 nodes, random horizontal placement
                int count = Random.Range(minNodesPerRow, maxNodesPerRow + 1);
                var xs = SampleDistinctX(count, nodeMinHorizontalGap);

                for (int i = 0; i < count; i++)
                    Graph.AddNode(r, new Vector2(xs[i], y));

                Graph.SortRowByX(r);
            }
        }

        // --- Connect each adjacent row pair with non-crossing edges ---
        for (int r = 0; r < totalRows - 1; r++)
            ConnectRowsPlanar(Graph.rows[r], Graph.rows[r + 1]);
    }


    // ------------------------------------------------------------
    // CONNECT ROWS (non-crossing)
    // ------------------------------------------------------------

    private void ConnectRowsPlanar(List<MapNodeData> fromRow, List<MapNodeData> toRow)
    {
        int nA = fromRow.Count;
        int nB = toRow.Count;
        if (nA == 0 || nB == 0)
            return;

        // Primary mapping: monotone (no crossings)
        for (int i = 0; i < nA; i++)
        {
            float t = (nA == 1) ? 0f : (float)i / (nA - 1);
            int j = Mathf.RoundToInt(t * (nB - 1));
            AddEdgeIfValid(fromRow[i], toRow[j]);
        }

        // Ensure each node in next row has at least one incoming
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

        // Optional: light extra neighbors (still non-crossing)
        for (int i = 0; i < nA; i++)
        {
            var from = fromRow[i];
            float t = (nA == 1) ? 0f : (float)i / (nA - 1);
            int center = Mathf.RoundToInt(t * (nB - 1));

            TryAddNeighbor(from, toRow, center - 1);
            TryAddNeighbor(from, toRow, center + 1);
        }
    }

    private void TryAddNeighbor(MapNodeData from, List<MapNodeData> toRow, int j)
    {
        if (j < 0 || j >= toRow.Count) return;
        if (Random.value > 0.5f) return; // keep density reasonable
        AddEdgeIfValid(from, toRow[j]);
    }

    private void AddEdgeIfValid(MapNodeData from, MapNodeData to)
    {
        if (from == null || to == null) return;
        if (to.rowIndex != from.rowIndex + 1) return;

        // Prevent duplicate edges
        if (from.outgoing.Contains(to.id))
            return;

        //  SPECIAL CASE: Start node (row 0) connects to *all* nodes in row 1
        if (from.rowIndex == 0)
        {
            // No degree limit here, just add the edge
            // (edges from the same point can't cross each other)
            Graph.AddEdge(from, to);
            return;
        }

        //  For all other rows, keep your max 2 in / max 2 out rules

        // Max 2 outgoing edges from any other node
        if (GetOutgoingCount(from) >= 2)
            return;

        // Max 2 incoming edges to any other node
        if (GetIncomingCount(to) >= 2)
            return;

        // Crossing check stays
        if (WouldCreateCrossing(from, to))
            return;

        Graph.AddEdge(from, to);
    }

    private bool HasIncomingFromPrevRow(MapNodeData to, List<MapNodeData> fromRow)
    {
        foreach (var from in fromRow)
            for (int k = 0; k < from.outgoing.Count; k++)
                if (from.outgoing[k] == to.id)
                    return true;
        return false;
    }

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

                // Decide visual type from EncounterType
                EncounterType visualType = node.encounterType;

                // If you want to force start/boss by row, you can override here:
                if (r == 0) visualType = EncounterType.Start;
                else if (r == Graph.totalRows - 1) visualType = EncounterType.Boss;

                // Style it
                if (mapSettings != null)
                    nodeBtn.ApplyStyle(mapSettings, visualType);

                // Set label from MapSettings style
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

                // Register + click hook
                nodeBtn.nodeId = node.id;
                nodeButtons[node.id] = nodeBtn;

                nodeBtn.button.onClick.RemoveAllListeners();
                int capturedId = node.id;
                nodeBtn.button.onClick.AddListener(() => OnNodeClicked(capturedId));

                nodeBtn.SetInteractable(false);
            }
        }
    }


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

                img.color = Color.white; // or your custom color if you add one later
            }

            img.raycastTarget = false;

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

    private void SetupStartNode()
    {
        if (Graph.rows.Count == 0 || Graph.rows[0].Count == 0)
            return;

        var start = Graph.rows[0][0];
        HighlightReachableEdges(Graph.rows[0][0]);
        // only this node clickable initially
        if (nodeButtons.TryGetValue(start.id, out var btn))
            btn.SetInteractable(true);

        currentNode = null; // you haven't chosen it until you click it
    }

    // ------------------------------------------------------------
    // TRAVERSAL LOGIC
    // ------------------------------------------------------------

    private void OnNodeClicked(int nodeId)
    {
        if (!nodeButtons.TryGetValue(nodeId, out var clickedBtn))
            return;

        var clicked = Graph.GetNodeById(nodeId);
        if (clicked == null) return;

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

            UpdateBottomInfo(); // <-- add this
            return;
        }



        // Only allow nodes connected from currentNode
        bool isConnected = false;
        foreach (var edge in Graph.edges)
        {
            if (edge.fromNodeId == currentNode.id && edge.toNodeId == clicked.id)
            {
                isConnected = true;

                // highlight travelled edge
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

        // Record time since last valid selection
        if (runActive)
        {
            float now = Time.time;
            float dt = now - lastClickTime;
            if (dt < 0f) dt = 0f;
            clickIntervals.Add(dt);
            lastClickTime = now;
        }

        // Fade unused outgoing edges from previous node
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

        // Move selection
        currentNode = clicked;
        clickedBtn.SetSelected();
        DisableAllNodes();
        EnableNextRow(clicked);
        HighlightReachableEdges(clicked);
        UpdateBottomInfo();

        // If it's the last row (boss), finish run
        if (clicked.rowIndex == Graph.totalRows - 1)
        {
            runActive = false;
            runCompleted = true;

            // finalize stats & push to BottomInfo
            UpdateBottomInfo();
            UnityEngine.Debug.Log("Reached final node.");
            DisableAllNodes();
        }


        // If last row: lock everything, path complete
        if (clicked.rowIndex == Graph.totalRows - 1)
        {
            UnityEngine.Debug.Log("Reached final node.");
            DisableAllNodes();
        }
    }

    private void DisableAllNodes()
    {
        foreach (var kvp in nodeButtons)
            kvp.Value.SetInteractable(false);
    }

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

    // Row 0 at TOP, last row at BOTTOM
    private float RowY(int r)
    {
        float h = mapArea.rect.height;
        float usable = h - topPadding - bottomPadding;
        float t = (totalRows <= 1) ? 0f : (float)r / (totalRows - 1); // 0..1

        // anchoredPosition: top = +h/2, bottom = -h/2
        return (h * 0.5f - topPadding) - t * usable;
    }

    private List<float> SampleDistinctX(int count, float minGap)
    {
        float minX = -mapArea.rect.width * 0.5f + leftPadding;
        float maxX = mapArea.rect.width * 0.5f - rightPadding;

        var xs = new List<float>(count);
        int safety = 0;

        while (xs.Count < count && safety < 2000)
        {
            safety++;
            float x = Random.Range(minX, maxX);

            bool ok = true;
            for (int i = 0; i < xs.Count; i++)
                if (Mathf.Abs(xs[i] - x) < minGap) { ok = false; break; }

            if (ok) xs.Add(x);
        }

        if (xs.Count < count)
        {
            xs.Clear();
            float step = (maxX - minX) / (count + 1);
            for (int i = 1; i <= count; i++)
                xs.Add(minX + step * i);
        }

        return xs;
    }
    private void HighlightReachableEdges(MapNodeData fromNode)
    {
        if (fromNode == null) return;

        // If dimming is OFF: show all edges at normal alpha, keep travelled edges colored
        if (!dimmingEnabled)
        {
            foreach (var kvp in edgeImages)
            {
                var img = kvp.Value;
                if (img == null) continue;

                // If this is a travelled edge (yellow or your travelled color), keep as-is
                if (img.color == travelledEdgeColor)
                    continue;

                var c = img.color;
                c.a = edgeAlpha;
                img.color = c;
            }

            return;
        }

        // Dimming is ON:

        // 1) Dim all non-travelled edges
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

        // 2) Brighten reachable edges from current node
        foreach (var edge in Graph.edges)
        {
            if (edge.fromNodeId == fromNode.id)
            {
                if (edgeImages.TryGetValue((edge.fromNodeId, edge.toNodeId), out var img))
                {
                    // If already travelled, keep travelled color
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

    private void UpdateBottomInfo()
    {
        if (bottomInfo == null)
            return;

        // Count encounter types + one-choice nodes
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

        // Last interval (seconds)
        float lastInterval = 0f;
        if (clickIntervals.Count > 0)
            lastInterval = clickIntervals[clickIntervals.Count - 1];

        // Totals only if run completed with at least one step
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
            optionCPassLastRun
        );
    }

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

        // Re-apply current highlight state
        if (currentNode != null)
            HighlightReachableEdges(currentNode);
        else if (Graph.rows.Count > 0 && Graph.rows[0].Count > 0)
            HighlightReachableEdges(Graph.rows[0][0]);
    }

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
        // We already store outgoing ids on the node
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

    private void AssignEncounters()
    {
        if (Graph == null || Graph.rows == null) return;

        optionAPassLastRun = false;
        optionCPassLastRun = false;

        switch (encounterMode)
        {
            case EncounterGenerationMode.OptionA:
                {
                    int encounterSeed = useFixedSeed
                        ? seed
                        : UnityEngine.Random.Range(int.MinValue, int.MaxValue);

                    var rng = new System.Random(encounterSeed);
                    optionAGenerator.Generate(Graph, rng, out optionAPassLastRun);
                    break;
                }

            // ... OptionB etc later

            case EncounterGenerationMode.OptionC:
                {
                    if (optionCSettings == null)
                    {
                        UnityEngine.Debug.LogWarning("Option C selected but optionCSettings is null.");
                        break;
                    }

                    int encounterSeed = useFixedSeed
                        ? seed
                        : UnityEngine.Random.Range(int.MinValue, int.MaxValue);

                    var rng = new System.Random(encounterSeed);
                    optionCGenerator.Generate(Graph, rng, optionCSettings, out optionCPassLastRun);
                    break;
                }
        }
    }

    public void SetEncounterMode(int index)
    {
        encounterMode = (EncounterGenerationMode)index;
    }
}
