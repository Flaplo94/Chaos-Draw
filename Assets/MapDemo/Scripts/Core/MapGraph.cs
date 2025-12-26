using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MapGraph
{
    /// Forventet antal rækker i grafen (bruges primært til layout og validering).
    public int totalRows;

    /// 2D-liste af noder organiseret pr. række. rows[rowIndex] er listen med MapNodeData for den række.
    public List<List<MapNodeData>> rows = new();

    /// Liste af kanter (direkte edges) i grafen. Hver MapEdgeData gemmer fra- og til-node id.
    public List<MapEdgeData> edges = new();

    // Internt id-generator for nye noder.
    private int _nextId;

    /// Ryd grafens indhold og nulstil intern id-tæller. Bruges før ny generering for at sikre en tom state.
    public void Clear()
    {
        rows.Clear();
        edges.Clear();
        _nextId = 0;
    }

    /// <summary>
    /// Opretter en ny node i en specificeret række og position (anchoredPos).
    /// - Sikrer at rows-listen har nok rækker til rowIndex.
    /// - Tildeler et unikt id.
    /// - Returnerer den oprettede MapNodeData.
    /// </summary>
    /// <param name="rowIndex">Rækkeindeks hvor noden skal placeres (0 = øverste række).</param>
    /// <param name="anchoredPos">Anchored position (UI/RectTransform koordinater) for noden.</param>
    public MapNodeData AddNode(int rowIndex, Vector2 anchoredPos)
    {
        // Udvid rows-listen hvis rowIndex endnu ikke eksisterer
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

    /// <summary>
    /// Sorter en given række efter X-koordinat (venstre->højre).
    /// Opdaterer kolonneindeks (colIndex) for alle noder i rækken.
    /// </summary>
    /// <param name="rowIndex">Index på rækken der skal sorteres.</param>
    public void SortRowByX(int rowIndex)
    {
        rows[rowIndex].Sort((a, b) => a.anchoredPos.x.CompareTo(b.anchoredPos.x));
        for (int i = 0; i < rows[rowIndex].Count; i++)
            rows[rowIndex][i].colIndex = i;
    }

    /// <summary>
    /// Tilføjer en rettet kant (edge) fra 'from' til 'to'.
    /// - Gemmer kant i edges-listen.
    /// - Lægger til 'to.id' i from.outgoing for hurtig navigation.
    /// </summary>
    /// <param name="from">Afsender-node.</param>
    /// <param name="to">Modtager-node.</param>
    public void AddEdge(MapNodeData from, MapNodeData to)
    {
        edges.Add(new MapEdgeData(from.id, to.id));
        from.outgoing.Add(to.id);
    }

    /// <summary>
    /// Find node-objektet for et givent node-id.
    /// - Går sekventielt gennem rows (effektivt for små grafer).
    /// - Returnerer null hvis id ikke findes.
    /// </summary>
    /// <param name="id">Node-id der søges efter.</param>
    /// <returns>MapNodeData eller null.</returns>
    public MapNodeData GetNodeById(int id)
    {
        for (int r = 0; r < rows.Count; r++)
            for (int i = 0; i < rows[r].Count; i++)
                if (rows[r][i].id == id) return rows[r][i];
        return null;
    }
}
