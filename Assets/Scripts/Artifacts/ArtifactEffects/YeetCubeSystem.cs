using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class YeetCubeSystem : MonoBehaviour
{
    // --- singleton ---
    private static YeetCubeSystem _inst;
    void Awake()
    {
        if (_inst == null) _inst = this;
        else if (_inst != this) { Destroy(gameObject); return; }
        DontDestroyOnLoad(gameObject);
    }

    public static void Enable(GameObject minePrefabArg = null)
    {
        if (_inst == null)
        {
            var go = new GameObject("YeetCubeSystem");
            _inst = go.AddComponent<YeetCubeSystem>();
            DontDestroyOnLoad(go);
        }

        if (minePrefabArg != null)
            _inst.minePrefab = minePrefabArg;

        Debug.Log($"[YeetCube] Enabled. MinePrefab={(_inst.minePrefab ? _inst.minePrefab.name : "NULL")}");
    }


    // --- config ---
    [Header("Perimeter Mines")]
    [SerializeField] private GameObject minePrefab;      // assign a prefab with YeetCubeMine (below)
    [SerializeField] private float mineInterval = 3f;
    [SerializeField] private int maxActiveMines = 6;
    [SerializeField] private Vector2 mineOffset = new Vector2(0.6f, -0.2f);

    [Header("Siphon Peck")]
    [SerializeField] private int hitsPerHeal = 7;        // heal 1 HP every 7th hit (boss counts as 3)
    [SerializeField] private int healAmount = 1;

    [Header("Critical Snowball")]
    [SerializeField] private float snowballStartMult = -0.15f; // -15% to start
    [SerializeField] private float snowballGainPerSec = 0.05f; // +5%/s while hitting
    [SerializeField] private float snowballMaxMult = 0.60f;    // +60% cap
    [SerializeField] private float snowballDropDelay = 5.0f;   // if no hits for 1s -> reset

    [Header("Ghost Walk")]
    [SerializeField] private bool ghostAffectsAllColliders = true; // set player's colliders to trigger

    [Header("Haste")]
    [SerializeField] private float hasteSpeedMult = 2f;

    [Header("Dice Weights (1..6)")]
    [Tooltip("Relative chance for each face. Higher = more likely. Godmode set a bit lower by default.")]
    [SerializeField] private float weightMines = 1f;   // 1
    [SerializeField] private float weightSiphon = 1f;   // 2
    [SerializeField] private float weightSnowball = 1f;   // 3
    [SerializeField] private float weightGhost = 1f;   // 4
    [SerializeField] private float weightHaste = 1f;   // 5
    [SerializeField] private float weightGodmode = 0.7f; // 6 (slightly rarer)


    // --- state ---
    public enum Effect { Mines = 1, Siphon = 2, Snowball = 3, Ghost = 4, Haste = 5, Godmode = 6 }
    public static Effect CurrentEffect { get; private set; } = 0;
    private Coroutine _minesLoop;
    private readonly List<GameObject> activeMines = new();

    // Siphon
    private int _hitTicker = 0;

    // Snowball
    private float _snowballMult = 0f;
    private float _lastHitTime = -999f;

    // Ghost
    private Collider2D[] _playerCols;
    private bool[] _wasTrigger;

    // Haste
    private bool _hasHasteApplied = false;

    // Godmode
    public static bool IsGodmodeActive { get; private set; } = false;

    // --- wave API ---
    public static void OnWaveStart()
    {
        if (_inst == null) return;
        _inst.RollAndApply();
    }
    public static void OnWaveEnd()
    {
        if (_inst == null) return;
        _inst.ClearEffect();
    }

    // --- hooks for damage reporting (call these from your damage code for best results) ---
    public static void ReportPlayerHit(Transform enemyRoot, bool isBoss = false)
    {
        if (_inst == null) return;

        // Siphon Peck: ONLY count hits while Siphon is the current effect
        if (CurrentEffect == Effect.Siphon)
        {
            _inst._hitTicker += isBoss ? 3 : 1;

            if (_inst._hitTicker >= _inst.hitsPerHeal)
            {
                _inst._hitTicker = 0;
                var hp = Object.FindFirstObjectByType<PlayerHealth>(FindObjectsInactive.Exclude);
                if (hp != null) hp.Heal(_inst.healAmount);
            }
        }

        // Critical Snowball: ONLY track last-hit time while Snowball is active
        if (CurrentEffect == Effect.Snowball)
            _inst._lastHitTime = Time.time;
    }


    // Caller-friendly sugar for your damage scripts: if you only know the root, we can infer boss flag.
    public static void ReportPlayerHit(Transform enemyRoot)
    {
        bool boss = enemyRoot && enemyRoot.GetComponent<BossHealth>() != null;
        ReportPlayerHit(enemyRoot, boss);
    }

    void Update()
    {
        // Snowball ramp/decay updates via PlayerBuffManager (if present)
        if (CurrentEffect == Effect.Snowball)
        {
            bool hitting = (Time.time - _lastHitTime) <= snowballDropDelay;
            float target = hitting ? snowballMaxMult : snowballStartMult;
            float rate = hitting ? snowballGainPerSec : Mathf.Abs(snowballStartMult) / 0.1f; // snap back quickly

            // Move multiplier toward target
            if (hitting)
                _snowballMult = Mathf.Min(snowballMaxMult, _snowballMult + snowballGainPerSec * Time.deltaTime);
            else
                _snowballMult = Mathf.MoveTowards(_snowballMult, snowballStartMult, rate * Time.deltaTime);

            ApplyDamageBuff(_snowballMult);
        }
    }

    // ================= core =================

    void RollAndApply()
    {
        ClearEffect();

        int roll = WeightedRoll();
        CurrentEffect = (Effect)roll;

        switch (CurrentEffect)
        {
            case Effect.Mines:
                _minesLoop = StartCoroutine(MinesRoutine());
                break;

            case Effect.Siphon:
                _hitTicker = 0;
                break;

            case Effect.Snowball:
                _snowballMult = snowballStartMult;
                _lastHitTime = -999f;
                ApplyDamageBuff(_snowballMult);
                break;

            case Effect.Ghost:
                EnableGhostWalk();
                break;

            case Effect.Haste:
                ApplyHaste(true);
                break;

            case Effect.Godmode:
                IsGodmodeActive = true;
                break;
        }

        ShowRollMessage(roll, CurrentEffect);

        Debug.Log($"[YeetCube] Wave roll = {roll} ({CurrentEffect})");
    }

    void ClearEffect()
    {
        // stop mines
        if (_minesLoop != null) { StopCoroutine(_minesLoop); _minesLoop = null; }
        foreach (var m in activeMines) if (m) Destroy(m);
        activeMines.Clear();

        // siphon: nothing to clear
        _hitTicker = 0;

        // snowball: remove buff
        ApplyDamageBuff(0f);
        _snowballMult = 0f;

        // ghost: revert colliders
        DisableGhostWalk();

        // haste: remove
        ApplyHaste(false);

        // godmode off
        IsGodmodeActive = false;
    }

    // ----- Perimeter Mines -----
    IEnumerator MinesRoutine()
    {
        while (true)
        {
            var player = FindPlayer();
            if (player)
            {
                // trim old
                activeMines.RemoveAll(x => x == null);
                while (activeMines.Count >= maxActiveMines)
                {
                    Destroy(activeMines[0]);
                    activeMines.RemoveAt(0);
                }

                if (minePrefab)
                {
                    Vector3 pos = player.position + (Vector3)mineOffset;
                    var mine = Instantiate(minePrefab, pos, Quaternion.identity);
                    activeMines.Add(mine);
                }
            }
            yield return new WaitForSeconds(mineInterval);
        }
    }

    // ----- Ghost Walk -----
    void EnableGhostWalk()
    {
        var player = FindPlayer();
        if (!player) return;

        _playerCols = player.GetComponentsInChildren<Collider2D>(true);
        if (_playerCols == null) return;

        _wasTrigger = new bool[_playerCols.Length];
        for (int i = 0; i < _playerCols.Length; i++)
        {
            _wasTrigger[i] = _playerCols[i].isTrigger;
            if (ghostAffectsAllColliders) _playerCols[i].isTrigger = true; // pass through solids & enemies
        }
        // Note: You will STILL take damage from enemy damage triggers, because triggers still fire.
    }

    void DisableGhostWalk()
    {
        if (_playerCols == null || _wasTrigger == null) return;
        for (int i = 0; i < _playerCols.Length && i < _wasTrigger.Length; i++)
            if (_playerCols[i]) _playerCols[i].isTrigger = _wasTrigger[i];

        _playerCols = null;
        _wasTrigger = null;
    }

    // ----- Haste -----
    void ApplyHaste(bool on)
    {
        if (on && !_hasHasteApplied)
        {
            var buffs = PlayerBuffManager.Instance;
            if (buffs != null)
            {
                buffs.AddRuntimeBonus(BuffData.BuffType.Speed, hasteSpeedMult - 1f); // +100% for x2
                _hasHasteApplied = true;
            }
        }
        else if (!on && _hasHasteApplied)
        {
            var buffs = PlayerBuffManager.Instance;
            if (buffs != null)
            {
                buffs.AddRuntimeBonus(BuffData.BuffType.Speed, -(hasteSpeedMult - 1f));
                _hasHasteApplied = false;
            }
        }
    }

    // ----- Snowball buff application -----
    float _lastAppliedBuff = 0f;
    void ApplyDamageBuff(float addMult)
    {
        // Translate multiplier to a "bonus" value compatible with your buff system
        // e.g., +0.20f = +20% damage; -0.15f = -15%
        var buffs = PlayerBuffManager.Instance;
        if (buffs == null) return;

        float delta = addMult - _lastAppliedBuff;
        if (Mathf.Abs(delta) > 0.0001f)
        {
            buffs.AddRuntimeBonus(BuffData.BuffType.Damage, delta);
            _lastAppliedBuff = addMult;
        }
    }

    // ----- helpers -----
    Transform FindPlayer()
    {
        var tagged = GameObject.FindGameObjectWithTag("Player");
        if (tagged) return tagged.transform;
        var pm = FindFirstObjectByType<PlayerMovement>(FindObjectsInactive.Exclude);
        return pm ? pm.transform : null;
    }

    // Show "You rolled a X: Effect Name" via your UIMessage
    void ShowRollMessage(int roll, Effect eff)
    {
        var ui = FindFirstObjectByType<UIMessage>(FindObjectsInactive.Exclude);
        if (ui != null)
            ui.ShowMessage($"You rolled a {roll}: {GetEffectLabel(eff)}");
        else
            Debug.Log($"[YeetCube] You rolled a {roll}: {GetEffectLabel(eff)}");
    }

    // Nice labels for each outcome
    string GetEffectLabel(Effect e) => e switch
    {
        Effect.Mines => "Perimeter Mines",
        Effect.Siphon => "Siphon Peck",
        Effect.Snowball => "Critical Snowball",
        Effect.Ghost => "Ghost Walk",
        Effect.Haste => "Haste (2× speed)",
        Effect.Godmode => "Godmode",
        _ => e.ToString()
    };

    int WeightedRoll()
    {
        // order = 1..6 (Mines, Siphon, Snowball, Ghost, Haste, Godmode)
        float[] w = {
        Mathf.Max(0f, weightMines),
        Mathf.Max(0f, weightSiphon),
        Mathf.Max(0f, weightSnowball),
        Mathf.Max(0f, weightGhost),
        Mathf.Max(0f, weightHaste),
        Mathf.Max(0f, weightGodmode)
    };

        float total = 0f;
        for (int i = 0; i < w.Length; i++) total += w[i];
        if (total <= 0f) return Random.Range(1, 7); // fallback to uniform if all zero

        float r = Random.value * total;
        float acc = 0f;
        for (int i = 0; i < w.Length; i++)
        {
            acc += w[i];
            if (r <= acc) return i + 1; // faces are 1..6
        }
        return 6; // safety
    }

}
