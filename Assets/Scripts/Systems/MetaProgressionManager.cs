using UnityEngine;

public class MetaProgressionManager : MonoBehaviour
{
    public static MetaProgressionManager Instance;

    [Header("Currency")]
    [SerializeField] private int chaosShards = 0;
    [SerializeField] private int xp = 0;

    [Header("Unlocks")]
    [SerializeField] public bool DeckOfFateUnlocked = false; // saved/loaded
    [SerializeField] public bool skillTreeUnlocked = false;  // saved/loaded

    // Optional UI hooks
    public static System.Action OnShardsChanged;
    public static System.Action OnXPChanged;

    // Run flag: was a wave>=10 boss defeated in this run
    private bool wave10BossDefeatedThisRun = false;

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }

        // VERSION RESET (behold hvis ønsket)
        string currentVersion = Application.version;
        string lastVersion = PlayerPrefs.GetString("lastBuildVersion", "");
        if (lastVersion != currentVersion)
        {
            Debug.Log("[Meta] New build detected (last=" + lastVersion + ", current=" + currentVersion + "). Resetting progression.");
            ResetAllMeta();
            PlayerPrefs.SetString("lastBuildVersion", currentVersion);
            PlayerPrefs.Save();
        }

        LoadData();
    }

    private void Update()
    {
        // Cheats for testing
        if (Input.GetKeyDown(KeyCode.Y))
        {
            AddShards(100);
            Debug.Log("[Cheat] +100 Chaos Shards");
        }
        if (Input.GetKeyDown(KeyCode.U))
        {
            AddXP(100);
            Debug.Log("[Cheat] +100 XP");
        }
        // Reset ALT (meta + skilltree levels) for hurtig test
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetAllMeta();
            var mgr = SkillTreeManager.Instance;
            if (mgr) mgr.ResetAllNodes();
            Debug.Log("[Cheat] RESET meta + skill tree");
        }
    }

    // ----- Currency: SHARDS -----
    public int GetShards() => chaosShards;

    public void AddShards(int baseAmount)
    {
        float mult = 1f;
        if (PlayerBuffManager.Instance != null)
            mult = PlayerBuffManager.Instance.GetChaosShardGainMult();

        int finalAmount = Mathf.RoundToInt(baseAmount * mult);
        chaosShards += finalAmount;
        SaveData();
        OnShardsChanged?.Invoke();
        Debug.Log("[Meta] +" + finalAmount + " shards (base " + baseAmount + ", mult " + mult + "). Total=" + chaosShards);
    }

    public bool SpendShards(int amount)
    {
        if (chaosShards < amount) return false;
        chaosShards -= amount;
        SaveData();
        OnShardsChanged?.Invoke();
        Debug.Log("[Meta] -" + amount + " shards. Total=" + chaosShards);
        return true;
    }

    // ----- Currency: XP -----
    public int GetXP() => xp;

    public void AddXP(int amount)
    {
        xp += Mathf.Max(0, amount);
        SaveData();
        OnXPChanged?.Invoke();
        Debug.Log("[Meta] +" + amount + " XP. Total=" + xp);
    }

    public bool SpendXP(int amount)
    {
        if (xp < amount) return false;
        xp -= amount;
        SaveData();
        OnXPChanged?.Invoke();
        Debug.Log("[Meta] -" + amount + " XP. Total=" + xp);
        return true;
    }

    // ----- Deck/SkillTree unlock flags -----
    public bool IsDeckOfFateUnlocked() => DeckOfFateUnlocked;

    public void UnlockDeckOfFate()
    {
        DeckOfFateUnlocked = true;
        Save();
        Debug.Log("[Meta] Deck of Fate unlocked");
    }

    public void Save() => SaveData();

    // ----- Run hooks (call from boss/player code) -----
    public void MarkWave10BossDefeated()
    {
        wave10BossDefeatedThisRun = true;
        Debug.Log("[Meta] Wave>=10 boss defeated. Flag set.");
    }

    public void TryUnlockSkillTreeOnDeath()
    {
        if (wave10BossDefeatedThisRun && !skillTreeUnlocked)
        {
            skillTreeUnlocked = true;
            Save();
            wave10BossDefeatedThisRun = false;
            Debug.Log("[Meta] Skill Tree UNLOCKED after death");
        }
    }

    public void ResetRunFlags() => wave10BossDefeatedThisRun = false;

    // ----- Save / Load -----
    private void SaveData()
    {
        PlayerPrefs.SetInt("chaosShards", chaosShards);
        PlayerPrefs.SetInt("xp", xp);
        PlayerPrefs.SetInt("DeckOfFateUnlocked", DeckOfFateUnlocked ? 1 : 0);
        PlayerPrefs.SetInt("skillTreeUnlocked", skillTreeUnlocked ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void LoadData()
    {
        chaosShards = PlayerPrefs.GetInt("chaosShards", 0);
        xp = PlayerPrefs.GetInt("xp", 0);
        DeckOfFateUnlocked = PlayerPrefs.GetInt("DeckOfFateUnlocked", 0) == 1;
        skillTreeUnlocked = PlayerPrefs.GetInt("skillTreeUnlocked", 0) == 1;
    }

    // ----- Testing helper -----
    [ContextMenu("Reset ALL Meta Progression (Testing)")]
    public void ResetAllMeta()
    {
        chaosShards = 0;
        xp = 0;
        skillTreeUnlocked = false;
        DeckOfFateUnlocked = false;

        PlayerPrefs.DeleteKey("chaosShards");
        PlayerPrefs.DeleteKey("xp");
        PlayerPrefs.DeleteKey("skillTreeUnlocked");
        PlayerPrefs.DeleteKey("DeckOfFateUnlocked");
        PlayerPrefs.DeleteKey("unlockedNodes"); // hvis SkillTreeManager bruger denne
        PlayerPrefs.DeleteKey("nodeLevels");    // hvis SkillTreeManager gemmer levels
        PlayerPrefs.Save();

        OnShardsChanged?.Invoke();
        OnXPChanged?.Invoke();

        Debug.Log("[Meta] All meta progression reset");
    }
}
