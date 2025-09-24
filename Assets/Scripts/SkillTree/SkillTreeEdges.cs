using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ChaosDraw.SkillTree;

[DefaultExecutionOrder(100)]
public class SkillTreeEdges : MonoBehaviour
{
    [Header("Hierarchy")]
    public RectTransform nodeParent;
    public RectTransform edgeLayer;

    [Header("Look")]
    public Sprite lineSprite;          // 1x1 hvid (valgfrit)
    public float thickness = 6f;

    [Header("Alpha (state)")]
    [Range(0, 1f)] public float lockedAlpha = 0.10f; // mørkere unavailable
    [Range(0, 1f)] public float availableAlpha = 0.55f;
    [Range(0, 1f)] public float unlockedAlpha = 1.00f;

    [Header("Refresh")]
    public bool autoRefresh = true;
    public float refreshInterval = 0.2f;

    class Edge { public Image img; public NodeData toNode; }
    readonly List<Edge> edges = new();

    void Start()
    {
        if (!nodeParent) nodeParent = transform.parent as RectTransform;
        if (!edgeLayer) edgeLayer = transform as RectTransform;
        TryRebuild();
    }

    void TryRebuild()
    {
        var mgr = SkillTreeManager.Instance;
        if (mgr == null || mgr.nodeBindings == null || mgr.nodeBindings.Length == 0)
        {
            Invoke(nameof(TryRebuild), 0f);
            return;
        }
        Rebuild();
    }

    [ContextMenu("Rebuild")]
    public void Rebuild()
    {
        for (int i = edgeLayer.childCount - 1; i >= 0; i--)
            DestroyImmediate(edgeLayer.GetChild(i).gameObject);
        edges.Clear();

        var mgr = SkillTreeManager.Instance;
        if (mgr == null || mgr.nodeBindings == null || mgr.nodeBindings.Length == 0) return;

        var map = new Dictionary<NodeData, RectTransform>();
        foreach (var b in mgr.nodeBindings)
            if (b.nodeUI && b.nodeData)
                map[b.nodeData] = b.nodeUI.transform as RectTransform;

        foreach (var b in mgr.nodeBindings)
        {
            if (b.nodeUI == null || b.nodeData == null) continue;
            var toRt = b.nodeUI.transform as RectTransform;
            var list = b.nodeData.prerequisites;
            if (list == null) continue;

            foreach (var pre in list)
            {
                if (pre == null) continue;
                if (!map.TryGetValue(pre, out var fromRt)) continue;
                CreateEdge(fromRt, toRt, b.nodeData);
            }
        }

        RefreshColors();
    }

    void CreateEdge(RectTransform from, RectTransform to, NodeData toNode)
    {
        Vector2 a = from.anchoredPosition;
        Vector2 b = to.anchoredPosition;
        Vector2 mid = (a + b) * 0.5f;
        float len = Vector2.Distance(a, b);
        float ang = Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;

        var go = new GameObject("edge_" + toNode.id, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(edgeLayer, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = mid;
        rt.sizeDelta = new Vector2(len, thickness);
        rt.localEulerAngles = new Vector3(0, 0, ang);
        go.transform.SetAsFirstSibling();

        var img = go.GetComponent<Image>();
        img.raycastTarget = false;
        img.type = Image.Type.Simple;
        if (lineSprite != null) img.sprite = lineSprite;

        edges.Add(new Edge { img = img, toNode = toNode });
    }

    float t;
    void Update()
    {
        if (edges.Count == 0)
        {
            var mgr = SkillTreeManager.Instance;
            if (mgr != null && mgr.nodeBindings != null && mgr.nodeBindings.Length > 0)
                TryRebuild();
        }

        if (!autoRefresh) return;
        t += Time.unscaledDeltaTime;
        if (t >= refreshInterval) { t = 0f; RefreshColors(); }
    }

    public void RefreshColors()
    {
        var mgr = SkillTreeManager.Instance;
        if (mgr == null) return;

        foreach (var e in edges)
        {
            bool u = mgr.IsUnlocked(e.toNode.id);
            bool avail = !u && mgr.ArePrerequisitesMet(e.toNode);

            // HUE = toNode.glowColor, alpha efter state
            Color c = (e.toNode != null) ? e.toNode.glowColor : Color.white;
            c.a = u ? unlockedAlpha : (avail ? availableAlpha : lockedAlpha);
            e.img.color = c;
        }
    }
}
