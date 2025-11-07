using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MapGraph
{
    public int totalRows;
    public List<List<MapNodeData>> rows = new();   // rows[r] => nodes in that row
    public List<MapEdgeData> edges = new();

    private int _nextId;

    public void Clear()
    {
        rows.Clear();
        edges.Clear();
        _nextId = 0;
    }

    public MapNodeData AddNode(int rowIndex, Vector2 anchoredPos)
    {
        while (rows.Count <= rowIndex) rows.Add(new List<MapNodeData>());

        var node = new MapNodeData
        {
            id = _nextId++,
            rowIndex = rowIndex,
            anchoredPos = anchoredPos
        };
        rows[rowIndex].Add(node);
        return node;
    }

    public void SortRowByX(int rowIndex)
    {
        rows[rowIndex].Sort((a, b) => a.anchoredPos.x.CompareTo(b.anchoredPos.x));
        for (int i = 0; i < rows[rowIndex].Count; i++)
            rows[rowIndex][i].colIndex = i;
    }

    public void AddEdge(MapNodeData from, MapNodeData to)
    {
        edges.Add(new MapEdgeData(from.id, to.id));
        from.outgoing.Add(to.id);
    }

    public MapNodeData GetNodeById(int id)
    {
        for (int r = 0; r < rows.Count; r++)
            for (int i = 0; i < rows[r].Count; i++)
                if (rows[r][i].id == id) return rows[r][i];
        return null;
    }
}
