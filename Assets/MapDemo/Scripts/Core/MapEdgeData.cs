using System;

// Dette script er lavet af Stefan
/// <summary>
/// Repræsenterer en kant i kortets graf (en directed edge).
/// Indeholder kun id'er til afsender- og modtager?noder.
/// Klassen er en simpel POD (plain old data) og er markeret som <see cref="Serializable"/>
/// så den kan vises/serialiseres af Unity.
/// </summary>
[Serializable]
public class MapEdgeData
{
    /// Id for den node kanten går fra (afsender). Bruges til opslag i <see cref="MapGraph"/> og ved opbygning af visuelle kanter.
    public int fromNodeId;

    /// Id for den node kanten går til (modtager).
    public int toNodeId;

    /// <summary>
    /// Opretter en kant mellem to node-id'er.
    /// </summary>
    /// <param name="from">Id for afsender?node.</param>
    /// <param name="to">Id for modtager?node.</param>
    public MapEdgeData(int from, int to)
    {
        fromNodeId = from;
        toNodeId = to;
    }
}
