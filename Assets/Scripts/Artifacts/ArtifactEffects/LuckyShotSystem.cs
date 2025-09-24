using System;
using UnityEngine;

/// Lucky Shot: every Nth cast duplicates immediately with a side offset.
/// Usage:
///  - When the artifact is gained: LuckyShotSystem.Enable(this); LuckyShotSystem.SetOffset(new Vector2(0.75f, 0f));
///  - After the first spawn+initialize of an ability:
///        LuckyShotSystem.OnSpellCast(this, () => { /* spawn duplicate */ });
///  - In the duplicate spawn block, before computing the position:
///        if (LuckyShotSystem.TryConsumeSpawnOffset(out var off)) spawnPos += (Vector3)off;
public static class LuckyShotSystem
{
    private static bool s_enabled;
    private static int s_castCounter;
    private static int s_triggerEvery = 5;

    private static Vector2 s_sideOffset = new Vector2(0.75f, 0f); // default horizontal offset
    private static Vector2? s_pendingOffset; // consumed by the duplicate spawner

    public static void Enable(MonoBehaviour host)
    {
        s_enabled = true;
        s_castCounter = 0;
        Debug.Log("[LuckyShot] Enabled (offset mode)");
    }

    public static void Disable()
    {
        s_enabled = false;
        s_castCounter = 0;
        s_pendingOffset = null;
    }

    public static void Configure(int triggerEvery = 5)
    {
        s_triggerEvery = Mathf.Max(1, triggerEvery);
    }

    public static void SetOffset(Vector2 sideOffset)
    {
        s_sideOffset = sideOffset;
    }

    // Convenience overload
    public static void OnSpellCast(Action duplicateCast) => OnSpellCast(null, duplicateCast);

    /// Call once per successful ability cast (right after first spawn+initialize).
    public static void OnSpellCast(MonoBehaviour host, Action duplicateCast)
    {
        if (!s_enabled || duplicateCast == null) return;

        s_castCounter++;
        if (s_castCounter < s_triggerEvery) return;
        s_castCounter = 0;

        // Immediate duplicate with side offset
        s_pendingOffset = s_sideOffset;
        duplicateCast();
        // Safety clear if the duplicate did not consume the offset
        s_pendingOffset = null;
    }

    /// Called by the duplicate spawn code to apply the offset to its spawn position.
    public static bool TryConsumeSpawnOffset(out Vector2 offset)
    {
        if (s_pendingOffset.HasValue)
        {
            offset = s_pendingOffset.Value;
            s_pendingOffset = null;
            return true;
        }
        offset = default;
        return false;
    }
}
