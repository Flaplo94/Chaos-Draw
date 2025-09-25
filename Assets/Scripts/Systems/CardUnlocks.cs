using UnityEngine;

/// <summary>
/// Central gating for card unlocks used by reward-pool.
/// Robust matching: checks both ability.abilityName and ability.name (asset name),
/// case-insensitive and ignoring spaces/_/-.
/// Default: all abilities are unlocked unless explicitly gated in GatedByDefault.
/// </summary>
public static class CardUnlocks
{
    // Cards that are LOCKED by default and must be unlocked by a node
    private static readonly string[] GatedByDefault = new string[]
    {
        "AOE Pulse",
        "Godspeed",
        // Add more gated cards here as you create them
    };

    // -------- Normalization helpers (case-insensitive, ignore space/_/-) --------
    private static string Normalize(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        s = s.ToUpperInvariant();
        // strip spaces, underscores, and dashes
        System.Text.StringBuilder sb = new System.Text.StringBuilder(s.Length);
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (c == ' ' || c == '_' || c == '-') continue;
            sb.Append(c);
        }
        return sb.ToString();
    }

    private static bool IsInGatedList(string nameOrId)
    {
        if (string.IsNullOrEmpty(nameOrId)) return false;
        string norm = Normalize(nameOrId);
        for (int i = 0; i < GatedByDefault.Length; i++)
        {
            if (Normalize(GatedByDefault[i]) == norm) return true;
        }
        return false;
    }

    // -------- PlayerPrefs keys (keep multiple forms for safety) --------
    private static string KeyRaw(string abilityName) => "unlock_card_" + abilityName;           // e.g. "AOE Pulse"
    private static string KeyAsset(string assetName) => "unlock_card_asset_" + assetName;       // e.g. asset .name
    private static string KeyNorm(string normalized) => "unlock_card_norm_" + normalized;       // e.g. "AOEPULSE"

    /// <summary>
    /// Returns true if the given ability is allowed to appear in rewards.
    /// Cards in GatedByDefault require an unlock flag; others are allowed by default.
    /// </summary>
    public static bool IsAbilityUnlocked(Ability ability)
    {
        if (ability == null) return false;

        string rawName = ability.abilityName; // from the Ability asset field
        string assetName = ability.name;        // Unity asset name
        string idForGate = !string.IsNullOrEmpty(rawName) ? rawName : assetName;

        // Only gate if the card is in the gated list
        if (!IsInGatedList(idForGate))
            return true;

        // Any of these flags being set means "unlocked"
        if (!string.IsNullOrEmpty(rawName) && PlayerPrefs.GetInt(KeyRaw(rawName), 0) == 1) return true;
        if (!string.IsNullOrEmpty(assetName) && PlayerPrefs.GetInt(KeyAsset(assetName), 0) == 1) return true;
        if (PlayerPrefs.GetInt(KeyNorm(Normalize(idForGate)), 0) == 1) return true;

        return false;
    }

    /// <summary>
    /// Unlock by the visible abilityName (what you show on the card).
    /// Also sets a normalized key for robustness.
    /// </summary>
    public static void UnlockAbilityByName(string abilityName)
    {
        if (string.IsNullOrEmpty(abilityName)) return;
        PlayerPrefs.SetInt(KeyRaw(abilityName), 1);
        PlayerPrefs.SetInt(KeyNorm(Normalize(abilityName)), 1);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// (Optional) Unlock by asset name if you prefer referencing the .name of the asset.
    /// </summary>
    public static void UnlockAbilityByAssetName(string assetName)
    {
        if (string.IsNullOrEmpty(assetName)) return;
        PlayerPrefs.SetInt(KeyAsset(assetName), 1);
        PlayerPrefs.SetInt(KeyNorm(Normalize(assetName)), 1);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// (Optional) Relock helpers for debugging.
    /// </summary>
    public static void LockAbilityByName(string abilityName)
    {
        if (string.IsNullOrEmpty(abilityName)) return;
        PlayerPrefs.DeleteKey(KeyRaw(abilityName));
        PlayerPrefs.DeleteKey(KeyNorm(Normalize(abilityName)));
        PlayerPrefs.Save();
    }

    public static void LockAbilityByAssetName(string assetName)
    {
        if (string.IsNullOrEmpty(assetName)) return;
        PlayerPrefs.DeleteKey(KeyAsset(assetName));
        PlayerPrefs.DeleteKey(KeyNorm(Normalize(assetName)));
        PlayerPrefs.Save();
    }
}
