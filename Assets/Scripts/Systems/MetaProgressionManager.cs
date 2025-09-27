using UnityEngine;
using UnityEngine.SceneManagement;

public class MetaProgressionManager : MonoBehaviour
{
    public static MetaProgressionManager Instance;

    [Header("Currency")]
    [SerializeField] private int chaosShards = 0;
    [SerializeField] private int xp = 0;

    [Header("Unlocks")]
    [SerializeField] public bool DeckOfFateUnlocked = false; // saved/loaded
    [SerializeField] public bool skillTreeUnlocked = false;  // saved/loaded

    public static System.Action OnShardsChanged;
    public static System.Action OnXPChanged;

    // ===== REROLL (per run) =====
    [Header("Run: Rerolls")]
    [SerializeField] private int rerollsCapacityThisRun = 0;   // 0..3 baseret på node-level
    [SerializeField] private int rerollsRemainingThisRun = 0;  // nedtælling pr. run
    private bool rerollsInitializedThisRun = false;

    // ===== Wave10-boss flag =====
    private bool wave10BossDefeatedThisRun = false;

    private const string KEY_NODE_LEVELS = "nodeLevels"; // samme key som SkillTreeManager bruger

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }

        LoadData();

        // Sørg for at et "nyt run" altid starter med friske rerolls – også ved Replay (scene reload)
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Vi betragter enhver gameplay scene reload som start på et nyt run.
        // Det er ufarligt at kalde det ved Menu også – det nulstiller blot run-state.
        StartNewRun();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Y)) { AddShards(100); }
        if (Input.GetKeyDown(KeyCode.U)) { AddXP(100); }
    }

    // ===== Currency =====
    public int GetShards() { return chaosShards; }
    public void AddShards(int baseAmount)
    {
        int finalAmount = Mathf.Max(0, baseAmount);
        chaosShards += finalAmount;
        SaveData();
        OnShardsChanged?.Invoke();
    }
    public bool SpendShards(int amount)
    {
        if (chaosShards < amount) return false;
        chaosShards -= amount;
        SaveData();
        OnShardsChanged?.Invoke();
        return true;
    }

    public int GetXP() { return xp; }
    public void AddXP(int amount)
    {
        xp += Mathf.Max(0, amount);
        SaveData();
        OnXPChanged?.Invoke();
    }
    public bool SpendXP(int amount)
    {
        if (xp < amount) return false;
        xp -= amount;
        SaveData();
        OnXPChanged?.Invoke();
        return true;
    }

    // ===== Deck/SkillTree unlock flags =====
    public bool IsDeckOfFateUnlocked() { return DeckOfFateUnlocked; }
    public void UnlockDeckOfFate() { DeckOfFateUnlocked = true; SaveData(); }

    public void UnlockSkillTreePermanent()
    {
        if (!skillTreeUnlocked)
        {
            skillTreeUnlocked = true;
            SaveData();
        }
    }

    public void Save() { SaveData(); }

    // ===== REROLL API =====
    public void StartNewRun()
    {
        int cap = 0;

        // 1) Brug SkillTreeManager hvis den findes i scenen
        var stm = SkillTreeManager.Instance;
        if (stm != null)
        {
            int lv = stm.GetLevel("card_reroll");
            cap = Mathf.Clamp(lv, 0, 3);
        }
        else
        {
            // 2) Fallback: læs direkte fra PlayerPrefs "nodeLevels" (format: id=level|id=level|...)
            string raw = PlayerPrefs.GetString(KEY_NODE_LEVELS, "");
            int lv = 0;
            if (!string.IsNullOrEmpty(raw))
            {
                var parts = raw.Split('|');
                for (int i = 0; i < parts.Length; i++)
                {
                    var kv = parts[i].Split('=');
                    if (kv.Length == 2 && kv[0] == "card_reroll")
                    {
                        int.TryParse(kv[1], out lv);
                        break;
                    }
                }
            }
            cap = Mathf.Clamp(lv, 0, 3);
        }

        rerollsCapacityThisRun = cap;
        rerollsRemainingThisRun = cap;
        rerollsInitializedThisRun = true;
        wave10BossDefeatedThisRun = false;
    }

    private void EnsureRerollsInitialized()
    {
        if (rerollsInitializedThisRun) return;
        StartNewRun();
    }

    public int GetRerollsRemaining()
    {
        EnsureRerollsInitialized();
        return rerollsRemainingThisRun;
    }

    public int GetRerollCapacity()
    {
        EnsureRerollsInitialized();
        return rerollsCapacityThisRun;
    }

    public bool TryConsumeReroll()
    {
        EnsureRerollsInitialized();
        if (rerollsRemainingThisRun <= 0) return false;
        rerollsRemainingThisRun--;
        return true;
    }

    // ===== Wave10 boss / skilltree unlock efter død =====
    public void MarkWave10BossDefeated()
    {
        wave10BossDefeatedThisRun = true;
    }

    public void TryUnlockSkillTreeOnDeath()
    {
        if (wave10BossDefeatedThisRun && !skillTreeUnlocked)
        {
            skillTreeUnlocked = true;
            SaveData();
            wave10BossDefeatedThisRun = false;
        }
    }

    // ===== Save / Load =====
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

    [ContextMenu("Reset Reroll For Next Run")]
    public void ResetRerollForNextRun()
    {
        rerollsInitializedThisRun = false;
        wave10BossDefeatedThisRun = false;
    }
}
