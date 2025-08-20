using System;

/// Artifact: Lucky Shot. Hver 5. cast bliver dobbelt-cast.
public static class LuckyShotSystem
{
    private static bool s_enabled;
    private static int s_castCounter;

    public static void Enable(UnityEngine.MonoBehaviour host)
    {
        s_enabled = true;
        s_castCounter = 0;
        UnityEngine.Debug.Log("[LuckyShot] Enabled");
    }

    /// Kald fra din cast-kode efter et succesfuldt cast.
    /// duplicateCast: delegate som kalder samme spell igen med samme parametre.
    public static void OnSpellCast(Action duplicateCast)
    {
        if (!s_enabled) return;
        s_castCounter++;
        if (s_castCounter >= 5)
        {
            s_castCounter = 0;
            if (duplicateCast != null)
            {
                UnityEngine.Debug.Log("[LuckyShot] Duplicate cast triggered");
                duplicateCast();
            }
        }
    }
}
