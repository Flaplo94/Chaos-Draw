using UnityEngine;
using UnityEngine.SceneManagement;

public class MetaProgressionManager : MonoBehaviour
{
    public static MetaProgressionManager Instance;

    [Header("Currency")]
    [SerializeField] private int chaosShards = 0;

    [Header("Unlocks")]
    [SerializeField] public bool legacyUnlocked = false;     // gemmes/loades
    [SerializeField] public bool skillTreeUnlocked = false;  // gemmes/loades

    // Event: UI kan lytte her
    public static System.Action OnShardsChanged;

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }

        // Force reset on new build
        string currentVersion = Application.version; // comes from Project Settings Player Version
        string lastVersion = PlayerPrefs.GetString("lastBuildVersion", "");

        if (lastVersion != currentVersion)
        {
            Debug.Log($"[MetaProgression] New build detected (last={lastVersion}, current={currentVersion}). Resetting progression.");
            ResetAllMeta();
            PlayerPrefs.SetString("lastBuildVersion", currentVersion);
            PlayerPrefs.Save();
        }

        LoadData();
        Debug.Log("[MetaProgression] Awake in scene: " +
            SceneManager.GetActiveScene().name);
    }

    void Update()
    {
        //  Cheat: tryk Y for +100 shards
        if (Input.GetKeyDown(KeyCode.Y))
        {
            AddShards(100);
            Debug.Log("[Cheat] +100 Chaos Shards (Y pressed)");
        }
    }

    // --- Currency API ---
    public int GetShards() => chaosShards;

    /// <summary>
    /// Tilføjer chaos shards – ganger op med multiplier fra PlayerBuffManager hvis tilgængelig.
    /// </summary>
    public void AddShards(int baseAmount)
    {
        float mult = 1f;
        if (PlayerBuffManager.Instance != null)
            mult = PlayerBuffManager.Instance.GetChaosShardGainMult();

        int finalAmount = Mathf.RoundToInt(baseAmount * mult);

        chaosShards += finalAmount;
        SaveData();

        Debug.Log($"[MetaProgression] Added {finalAmount} Chaos Shards (base {baseAmount}, mult {mult}). Total = {chaosShards}");
        OnShardsChanged?.Invoke();
    }

    /// <summary>
    /// Forsøg at bruge shards – returnerer false hvis man ikke har nok.
    /// </summary>
    public bool SpendShards(int amount)
    {
        if (chaosShards < amount) return false;
        chaosShards -= amount;
        SaveData();
        Debug.Log($"-{amount} Chaos Shards. Remaining: {chaosShards}");
        OnShardsChanged?.Invoke();
        return true;
    }

    // --- Save/Load ---
    public void Save()
    {
        SaveData();
    }

    void SaveData()
    {
        PlayerPrefs.SetInt("chaosShards", chaosShards);
        PlayerPrefs.SetInt("legacyUnlocked", legacyUnlocked ? 1 : 0);
        PlayerPrefs.SetInt("skillTreeUnlocked", skillTreeUnlocked ? 1 : 0);
        PlayerPrefs.Save();
    }

    void LoadData()
    {
        chaosShards = PlayerPrefs.GetInt("chaosShards", 0);
        legacyUnlocked = PlayerPrefs.GetInt("legacyUnlocked", 0) == 1;
        skillTreeUnlocked = PlayerPrefs.GetInt("skillTreeUnlocked", 0) == 1;
    }

    // --- Legacy wrappers (for old calls) ---
    public bool IsLegacyUnlocked() => legacyUnlocked;

    public void UnlockLegacy()
    {
        legacyUnlocked = true;
        Save();
        Debug.Log("[MetaProgression] Legacy Deck unlocked (via wrapper).");
    }

    // --- TESTING: Reset alt metaprogression ---
    [ContextMenu("Reset ALL Meta Progression (Testing)")]
    public void ResetAllMeta()
    {
        // Reset alle værdier
        chaosShards = 0;
        skillTreeUnlocked = false;
        legacyUnlocked = false;

        // Ryd PlayerPrefs nøgler
        PlayerPrefs.DeleteKey("chaosShards");
        PlayerPrefs.DeleteKey("skillTreeUnlocked");
        PlayerPrefs.DeleteKey("legacyUnlocked");
        PlayerPrefs.DeleteKey("lightningUnlocked");
        PlayerPrefs.DeleteKey("unlockedSkills"); // fra SkillTreeManager
        PlayerPrefs.Save();

        Debug.Log("[MetaProgressionManager] All meta progression reset!");
    }
}
