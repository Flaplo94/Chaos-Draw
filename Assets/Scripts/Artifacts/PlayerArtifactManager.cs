using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerArtifactManager : MonoBehaviour
{
    public static PlayerArtifactManager Instance;

    [Header("Scene refs")]
    [SerializeField] private Transform player;                 // Assign in Inspector or auto-found in Awake

    [Header("Prefabs")]
    [SerializeField] private GameObject holyCheeseOrbPrefab;   // Prefab with SpriteRenderer + CircleCollider2D (trigger) + Rigidbody2D (Kinematic) + HolyCheeseOrbiter

    public List<ArtifactData> ownedArtifacts = new List<ArtifactData>();
    private readonly HashSet<string> appliedIDs = new HashSet<string>();

    // Runtime
    private GameObject holyCheeseInstance;

    [SerializeField] private GameObject kamikazeBananaProjectilePrefab;
    [SerializeField] private float bananaIntervalSeconds = 15f;

    // cache
    private Coroutine bananaLoop;

    [SerializeField] private GameObject error404ExplosionPrefab;


    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        // Auto-find player if not wired
        if (player == null)
        {
            var tagged = GameObject.FindGameObjectWithTag("Player");
            if (tagged) player = tagged.transform;

            if (player == null)
            {
                // Unity 6 recommended: FindFirstObjectByType
                var pm = FindFirstObjectByType<PlayerMovement>(FindObjectsInactive.Exclude);
                if (pm != null) player = pm.transform;
            }
        }
    }

    public void AddArtifact(ArtifactData artifact)
    {
        if (artifact == null) return;

        string key = Normalize(artifact.internalID);
        if (appliedIDs.Contains(key)) { Debug.Log("[Artifact] ID already applied: " + key); return; }

        if (!ownedArtifacts.Contains(artifact))
        {
            ownedArtifacts.Add(artifact);
            appliedIDs.Add(key);

            ApplyArtifactEffect(artifact);

            var ui = FindFirstObjectByType<ArtifactUIManager>();
            if (ui != null) ui.UpdateArtifactUI();
        }
    }

    public bool HasArtifact(string id)
    {
        string nid = Normalize(id);
        return ownedArtifacts.Exists(a => a != null && Normalize(a.internalID) == nid);
    }

    private void ApplyArtifactEffect(ArtifactData a)
    {
        var buffs = PlayerBuffManager.Instance;
        var hp = FindFirstObjectByType<PlayerHealth>();
        if (buffs == null) { Debug.LogError("[Artifact] PlayerBuffManager not found!"); return; }

        string id = Normalize(a.internalID);

        switch (id)
        {
            // Economy
            case "goldenidol":
                buffs.AddRuntimeBonus(BuffData.BuffType.GoldGain, 0.50f);
                break;

            // Charcoal = +40% fire damage
            case "charcoal":
                buffs.AddRuntimeBonus(BuffData.BuffType.FireDamage, 0.40f);
                break;

            case "zapstick3000":
                buffs.AddRuntimeBonus(BuffData.BuffType.ThunderDamage, 0.40f);
                break;

            // GamersCap = +10% på alt
            case "gamerscap":
                float v = 0.10f;
                buffs.AddRuntimeBonus(BuffData.BuffType.Damage, v);
                buffs.AddRuntimeBonus(BuffData.BuffType.FireDamage, v);
                buffs.AddRuntimeBonus(BuffData.BuffType.ThunderDamage, v);
                buffs.AddRuntimeBonus(BuffData.BuffType.BurnDamage, v);
                buffs.AddRuntimeBonus(BuffData.BuffType.MaxHP, v);
                buffs.AddRuntimeBonus(BuffData.BuffType.Speed, v);
                buffs.AddRuntimeBonus(BuffData.BuffType.AttackSpeed, v);
                buffs.AddRuntimeBonus(BuffData.BuffType.GoldGain, v);
                break;

            // GlassCannon = triple damage + set HP to 1
            case "glasscannon":
                buffs.AddRuntimeBonus(BuffData.BuffType.Damage, 3.00f);
                if (hp != null) hp.ForceSetToOneHP();
                break;

            case "luckyshot":
                LuckyShotSystem.Enable(this);
                LuckyShotSystem.SetOffset(new Vector2(0.75f, 0f));
                break;

            case "sparerib":
                if (hp != null) hp.AddExtraLife(1);
                break;

            case "holycheese":
                ApplyHolyCheese(a);
                break;

            case "kamikazebanana":
                if (bananaLoop == null) bananaLoop = StartCoroutine(KamikazeBananaLoop());
                break;

            case "chickenegg":
                PetEggSystem.Enable(this);
                break;

            case "error404":
                Error404ProcSystem.Enable(error404ExplosionPrefab);
                break;

            case "deckless":
                DecklessSystem.Enable();
                break;

            case "braindamage":
                buffs.AddRuntimeBonus(BuffData.BuffType.ManaCostReduction, 999f);
                InputShuffleSystem.ShuffleKeys();
                break;

            case "yeetcube":
                YeetCubeSystem.Enable();
                break;

            default:
                Debug.LogWarning($"[Artifact] No effect defined for '{a.internalID}' (normalized='{id}')");
                break;
        }
    }

    private void ApplyHolyCheese(ArtifactData data)
    {
        if (player == null)
        {
            Debug.LogWarning("HolyCheese: player Transform not found.");
            return;
        }
        if (holyCheeseInstance != null) return; // already spawned

        if (holyCheeseOrbPrefab == null)
        {
            Debug.LogWarning("HolyCheese: prefab not assigned on PlayerArtifactManager.");
            return;
        }

        holyCheeseInstance = Instantiate(holyCheeseOrbPrefab, player); // parent under player

        var orb = holyCheeseInstance.GetComponent<HolyCheeseOrbiter>();
        if (orb != null)
        {
            orb.owner = player; // orbit around the player
            // damage/radius/speed are tuned on the prefab via inspector
        }
    }

    public void RemoveHolyCheese()
    {
        if (holyCheeseInstance != null)
        {
            Destroy(holyCheeseInstance);
            holyCheeseInstance = null;
        }
    }

    static string Normalize(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        s = s.Trim().ToLowerInvariant();
        s = s.Replace(" ", "").Replace("_", "").Replace("-", "");
        return s;
    }

    private IEnumerator KamikazeBananaLoop()
    {
        // waits first so it doesn't fire instantly on pickup; change if you want instant fire
        while (HasArtifact("kamikazebanana"))
        {
            yield return new WaitForSeconds(bananaIntervalSeconds);
            FireKamikazeBanana();
        }
        bananaLoop = null;
    }

    private void FireKamikazeBanana()
    {
        // get player transform (this manager lives on GameManager)
        Transform player = GetPlayerTransform();
        if (player == null || kamikazeBananaProjectilePrefab == null) return;

        Transform target = PickRandomEnemyTarget();
        if (target == null) return;

        var proj = Instantiate(kamikazeBananaProjectilePrefab, player.position, Quaternion.identity);
        var kb = proj.GetComponent<KamikazeBananaProjectile>();
        if (kb != null) kb.target = target;
    }

    // Get the player transform the same way you already do elsewhere
    private Transform GetPlayerTransform()
    {
        if (player != null) return player;

        var tagged = GameObject.FindGameObjectWithTag("Player");
        if (tagged) return tagged.transform;

        var pm = FindFirstObjectByType<PlayerMovement>(FindObjectsInactive.Exclude);
        return pm ? pm.transform : null;
    }

    private Transform PickRandomEnemyTarget()
    {
        // Prefer any living enemy; include bosses
        var enemies = FindObjectsByType<EnemyHealth>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        var bosses = FindObjectsByType<BossHealth>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        int total = enemies.Length + bosses.Length;
        if (total == 0) return null;

        int idx = Random.Range(0, total);
        if (idx < enemies.Length) return enemies[idx].transform;
        return bosses[idx - enemies.Length].transform;
    }

}
