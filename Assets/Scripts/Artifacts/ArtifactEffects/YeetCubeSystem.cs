using UnityEngine;

/// Artifact: Yeet Cube. Random event ved wave-start (stub).
public static class YeetCubeSystem
{
    public static bool Enabled { get; private set; }

    public static void Enable()
    {
        Enabled = true;
        Debug.Log("[YeetCube] Enabled - lyt paa wave-start og rul en terning");
    }

    /// Kald denne fra din WaveManager naar en ny wave starter
    public static void OnWaveStart()
    {
        if (!Enabled) return;
        int roll = Random.Range(1, 7);
        Debug.Log("[YeetCube] Rolled: " + roll);
        // TODO: Implementer effekter for 1-6
    }
}
