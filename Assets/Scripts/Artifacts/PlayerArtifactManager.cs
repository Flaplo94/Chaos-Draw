using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerArtifactManager : MonoBehaviour
{
    public static PlayerArtifactManager Instance;

    public List<ArtifactData> ownedArtifacts = new List<ArtifactData>();
    private readonly HashSet<string> appliedIDs = new HashSet<string>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
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
                OrbiterSystem.SpawnCheese(transform);
                break;

            case "kamikazebanana":
                StartCoroutine(BananaRoutine());
                break;

            case "chickenegg":
                PetEggSystem.Enable(this);
                break;

            case "error404":
                Error404ProcSystem.Enable();
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

    static string Normalize(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        s = s.Trim().ToLowerInvariant();
        s = s.Replace(" ", "").Replace("_", "").Replace("-", "");
        return s;
    }
    private IEnumerator BananaRoutine()
    {
        while (HasArtifact("KamikazeBanana"))
        {
            yield return new WaitForSeconds(15f);
            BananaSpawner.ThrowAtRandomEnemy(transform.position);
        }
    }
}
