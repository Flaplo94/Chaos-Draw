using System;
using System.Collections.Generic;
using UnityEngine;

// Dette script er lavet af Stefan
/// Typer af encounters som en node kan repræsentere. Bruges til styling, encounter-logik og generatoralgoritmer.
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

/// <summary>
/// Data-holder for en enkelt node i kortets graf (MapGraph).
/// Indeholder identifikation, layout?position, links til næste noder og hvilken encounter?type noden repræsenterer.
/// Denne klasse er en ren data?klasse (POD) — logik holdes ude af modellen.
/// </summary>
[Serializable]
public class MapNodeData
{
    /// Unik id for noden (bruges i Graph til opslag og kanter).
    public int id;

    /// Rækkeindeks (row) i kortlayoutet. 0 = øverste række (start).
    public int rowIndex;

    /// Kolonneindeks (col) inden for rækken. Bruges ved sortering/placering.
    public int colIndex;

    /// Position i UI (anchored position i RectTransform?koordinater). Bruges når node UI elementer skal placeres i mapArea.
    public Vector2 anchoredPos;

    /// Liste af node?id'er som denne node har udgående kanter til. Gemmer referencer med id (ikke direkte GameObject?referencer).
    public List<int> outgoing = new List<int>();

    /// Hvilken encounter?type noden er tildelt. Standard er Normal. Påvirker både visuel styling og encounter?generatorens logik.
    public EncounterType encounterType = EncounterType.Normal;
}
