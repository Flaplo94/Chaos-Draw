using System;
using System.Collections.Generic;
using UnityEngine;

public enum EncounterType
{
    Start,
    Normal,
    Special,
    Elite,
    Event,
    Shop,
    Boss
}

[Serializable]
public class MapNodeData
{
    public int id;                 // unique per map
    public int rowIndex;           // 0..(totalRows-1)
    public int colIndex;           // index within its row (after x-sort)
    public Vector2 anchoredPos;    // UI anchoredPosition in the map area

    // Neighbors one row below (DAG: edges always go r -> r+1)
    public List<int> outgoing = new List<int>();

    public EncounterType encounterType = EncounterType.Normal;
}
