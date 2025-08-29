using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerArtifactManager : MonoBehaviour
{
    public static PlayerArtifactManager Instance;

    public List<ArtifactData> ownedArtifacts = new List<ArtifactData>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void AddArtifact(ArtifactData artifact)
    {
        if (artifact == null) return;
        if (!ownedArtifacts.Contains(artifact))
        {
            ownedArtifacts.Add(artifact);
            Debug.Log($"[Artifact] Added: {artifact.artifactName} (ID={artifact.internalID})");

            ApplyArtifactEffect(artifact);

            var ui = FindFirstObjectByType<ArtifactUIManager>();
            if (ui != null) ui.UpdateArtifactUI();
        }
        else
        {
            Debug.Log($"[Artifact] Already owned: {artifact.artifactName}");
        }
    }

    public bool HasArtifact(string id)
    {
        return ownedArtifacts.Exists(a => a != null && a.internalID == id);
    }

    private void ApplyArtifactEffect(ArtifactData a)
    {
        var buffs = PlayerBuffManager.Instance;
        var hp = FindFirstObjectByType<PlayerHealth>();

        if (buffs == null)
        {
            Debug.LogError("[Artifact] PlayerBuffManager not found!");
            return;
        }

        switch (a.internalID)
        {
            // Economy
            case "GoldenIdol":
                buffs.AddRuntimeBonus(BuffData.BuffType.GoldGain, 0.50f); // +50% gold
                break;

            //  Charcoal = +40% fire damage
            case "Charcoal":
                buffs.AddRuntimeBonus(BuffData.BuffType.FireDamage, 0.40f);
                break;

            //  GamersCap = +10% på alt
            case "GamersCap":
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

            //  GlassCannon = triple damage men 1 HP
            case "GlassCannon":
                buffs.AddRuntimeBonus(BuffData.BuffType.Damage, 3.00f);
                if (hp != null) hp.ForceSetToOneHP();
                break;

            //  LuckyShot = double cast hver 5.
            case "LuckyShot":
                LuckyShotSystem.Enable(this);
                break;

            //  SpareRib = +1 liv
            case "SpareRib":
                if (hp != null) hp.AddExtraLife(1);
                break;

            //  Orbiters / pets
            case "HolyCheese":
                OrbiterSystem.SpawnCheese(transform);
                break;

            case "KamikazeBanana":
                StartCoroutine(BananaRoutine());
                break;

            case "ChickenEgg":
                PetEggSystem.Enable(this);
                break;

            case "Error404":
                Error404ProcSystem.Enable();
                break;

            //  Deckless = speciel effekt
            case "Deckless":
                DecklessSystem.Enable();
                break;

            //  BrainDamage = næsten gratis spells + shuffle keys
            case "BrainDamage":
                buffs.AddRuntimeBonus(BuffData.BuffType.ManaCostReduction, 999f);
                InputShuffleSystem.ShuffleKeys();
                break;

            //  YeetCube = random wave events
            case "YeetCube":
                YeetCubeSystem.Enable();
                break;

            default:
                Debug.LogWarning($"[Artifact] No effect defined for {a.internalID}");
                break;
        }
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
