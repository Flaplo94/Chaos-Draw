using System;
using UnityEngine;

// Dette script er lavet af Marc
/// <summary>
/// Indstillinger for "Option C" encounter-generatoren.
/// Denne serializable klasse indeholder:
/// - point-cost per encounter-type (bruges af generatoren ved beregning),
/// - et budget pr række (index = rowIndex) der styrer hvor mange point der kan fordeles i den række.
/// 
/// Brug i Inspector: opret en instans (eller rediger den indlejrede settings-variabel) og konfigurer costs og rowBudgets.
/// Rækker 0 (start) og sidste række (boss) ignoreres af generatoren; budgets anvendes for rækker 1..last-1.
/// </summary>
[Serializable]
public class OptionCSettings
{
    [Header("Encounter costs (points per node)")]
    /// Pointkost for en normal node. Standard 1 point. Generatoren bruger disse værdier til at beregne kombinationer som opfylder en radens budget.
    public int costNormal = 1;

    /// Pointkost for en elite node. Standard 4 point.
    public int costElite = 4;

    /// Pointkost for en special node. Standard 3 point.
    public int costSpecial = 3;

    /// Pointkost for en event node. Standard 2 point.
    public int costEvent = 2;

    /// Pointkost for en shop node. Standard 2 point.
    public int costShop = 2;

    [Header("Budget per row (index = rowIndex)")]
    [Tooltip("Set one element per row. Rows 0 (start) and last (boss) are ignored, budgets used for rows 1..last-1.")]
    /// <summary>
    /// Budget pr række. Hvert element angiver hvor mange point der er tilgængelige for den pågældende række.
    /// Længden af arrayet bør mindst dække antallet af rækker i et kort (MapGraph.totalRows).
    /// Række 0 og sidste række ignoreres typisk.
    /// </summary>
    public int[] rowBudgets = new int[15];
}
