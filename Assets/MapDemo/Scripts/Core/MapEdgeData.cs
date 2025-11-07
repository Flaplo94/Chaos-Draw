using System;

[Serializable]
public class MapEdgeData
{
    public int fromNodeId;
    public int toNodeId;

    public MapEdgeData(int from, int to)
    {
        fromNodeId = from;
        toNodeId = to;
    }
}
