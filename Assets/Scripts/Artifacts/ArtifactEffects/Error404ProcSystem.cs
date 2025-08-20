using UnityEngine;

/// Artifact: Error 404. Stub der kan procces en eksplosition i ny og nae.
public static class Error404ProcSystem
{
    private static bool s_enabled;

    public static void Enable()
    {
        s_enabled = true;
        Debug.Log("[Error404] Enabled");
    }

    /// Kald evt. denne fra et passende sted (fx per sekund paa enemies) for at teste
    public static void TryProcAt(Vector3 pos, float chancePerCall = 0.02f)
    {
        if (!s_enabled) return;
        if (Random.value <= chancePerCall)
        {
            // TODO: Rigtig explosion FX
            Debug.Log("[Error404] BOOM at " + pos);
        }
    }
}
