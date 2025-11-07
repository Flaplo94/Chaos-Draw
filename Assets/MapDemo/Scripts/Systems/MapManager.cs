using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RectTransform mapArea;    // Area over the parchment
    [SerializeField] private RectTransform nodesParent;
    [SerializeField] private RectTransform edgesParent;
    [SerializeField] private GameObject nodePrefab;    // UI Button/Image (RectTransform + Button + Image)
    [SerializeField] private GameObject edgePrefab;    // Prefab with an Image (root or child)

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

    // Data
    public MapGraph Graph { get; private set; } = new MapGraph();

    // State for traversal
    private MapNodeData currentNode;
    private readonly Dictionary<int, MapNodeButton> nodeButtons = new();
    private readonly Dictionary<(int fromId, int toId), Image> edgeImages = new();

    // Visual references (for cleanup)
    private readonly List<GameObject> spawnedNodes = new();
    private readonly List<Image> spawnedEdges = new();

    // ------------------------------------------------------------
    // PUBLIC API
    // ------------------------------------------------------------

    // Called by Regenerate button
    public void RegenerateMap()
    {
        ClearAllRuntime();
        GenerateGraph();
        BuildEdges();
        BuildNodes();
        SetupStartNode();
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

        // 1) Create nodes per row (top row index 0, bottom last)
        for (int r = 0; r < totalRows; r++)
        {
            int count = (r == 0 || r == totalRows - 1)
                ? 1
                : Random.Range(minNodesPerRow, maxNodesPerRow + 1);

            var xs = SampleDistinctX(count, nodeMinHorizontalGap);
            float y = RowY(r);

            for (int i = 0; i < count; i++)
                Graph.AddNode(r, new Vector2(xs[i], y));

            Graph.SortRowByX(r);
        }

        // 2) Connect each adjacent row pair with non-crossing edges
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
        if (to.rowIndex != from.rowIndex + 1) return; // only next row

        // prevent duplicates
        for (int k = 0; k < from.outgoing.Count; k++)
            if (from.outgoing[k] == to.id)
                return;

        // prevent crossings with existing edges in same row pair
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

        for (int r = 0; r < Graph.rows.Count; r++)
        {
            foreach (var node in Graph.rows[r])
            {
                var go = Instantiate(nodePrefab, nodesParent);
                spawnedNodes.Add(go);

                var rt = go.GetComponent<RectTransform>();
                if (rt == null)
                {
                    Debug.LogError("Node prefab requires a RectTransform on the root.");
                    continue;
                }

                rt.anchoredPosition = node.anchoredPos;

                var nodeBtn = go.GetComponent<MapNodeButton>();
                if (nodeBtn == null)
                    nodeBtn = go.AddComponent<MapNodeButton>();

                if (nodeBtn.button == null)
                    nodeBtn.button = go.GetComponent<Button>();
                if (nodeBtn.button == null)
                {
                    Debug.LogError("Node prefab requires a Button on the root.");
                    continue;
                }

                if (nodeBtn.background == null)
                    nodeBtn.background = go.GetComponent<Image>();

                nodeBtn.nodeId = node.id;
                nodeButtons[node.id] = nodeBtn;

                nodeBtn.button.onClick.RemoveAllListeners();
                nodeBtn.button.onClick.AddListener(() => OnNodeClicked(node.id));
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
                Debug.LogError("Edge prefab must have an Image.");
                Destroy(go);
                continue;
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

        // First selection: must be top row
        if (currentNode == null)
        {
            if (clicked.rowIndex != 0)
            {
                Debug.Log("You must start at the top node.");
                return;
            }

            currentNode = clicked;
            clickedBtn.SetSelected();
            DisableAllNodes();
            EnableNextRow(clicked);
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
            Debug.Log("That node is not connected to your current node.");
            return;
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

        // If last row: lock everything, path complete
        if (clicked.rowIndex == Graph.totalRows - 1)
        {
            Debug.Log("Reached final node.");
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
}
