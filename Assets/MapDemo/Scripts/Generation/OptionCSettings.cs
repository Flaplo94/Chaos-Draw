using System;
using UnityEngine;

/// <summary>
/// Settings for Option C: encounter costs and per-row budgets.
/// This is just a data container, shown in the inspector on MapManager.
/// </summary>
[Serializable]
public class OptionCSettings
{
    [Header("Encounter costs (points per node)")]
    public int costNormal = 1;
    public int costElite = 4;
    public int costSpecial = 3;
    public int costEvent = 2;
    public int costShop = 2;

    [Header("Budget per row (index = rowIndex)")]
    [Tooltip("Set one element per row. Rows 0 (start) and last (boss) are ignored, budgets used for rows 1..last-1.")]
    public int[] rowBudgets = new int[15];   // default for your 15-row map
}
