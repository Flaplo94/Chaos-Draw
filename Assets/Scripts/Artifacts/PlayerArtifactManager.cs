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
            Debug.Log("Artifact added: " + artifact.artifactName);

            ApplyArtifactEffect(artifact);

            var ui = FindFirstObjectByType<ArtifactUIManager>();
            if (ui != null) ui.UpdateArtifactUI();
        }
    }

    public bool HasArtifact(string id)
    {
        return ownedArtifacts.Exists(a => a != null && a.internalID == id);
    }

    void ApplyArtifactEffect(ArtifactData a)
    {
        var buffs = PlayerBuffManager.Instance;
        var hp = FindFirstObjectByType<PlayerHealth>(); // replace with your own health script

        switch (a.internalID)
        {
            // Economy / multipliers
            case "GoldenIdol":
                // +50% gold
                buffs.AddRuntimeBonus(BuffData.BuffType.GoldGain, 0.50f);
                break;

            // Elements
            case "Charcoal":
                // +40% fire damage
                buffs.AddRuntimeBonus(BuffData.BuffType.FireDamage, 0.40f);
                break;

            // Global stat bump
            case "GamersCap":
                {
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
                }

            // Glass cannon
            case "GlassCannon":
                buffs.AddRuntimeBonus(BuffData.BuffType.Damage, 3.00f); // +300%
                if (hp != null) hp.ForceSetToOneHP();
                break;

            // Lucky Shot (every 5th cast double-casts)
            case "LuckyShot":
                LuckyShotSystem.Enable(this);
                break;

            // Extra life
            case "SpareRib":
                if (hp != null) hp.AddExtraLife(1);
                break;

            // Orbiters / pets / procs (stubs)
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

            // Deck / wave hooks
            case "Deckless":
                DecklessSystem.Enable();
                break;

            // Mana cost and input shuffle
            case "BrainDamage":
                // Force near free spells. Use a large reduction to clamp at >= 0 later.
                buffs.AddRuntimeBonus(BuffData.BuffType.ManaCostReduction, 999f);
                InputShuffleSystem.ShuffleKeys();
                break;

            // Random wave start events
            case "YeetCube":
                YeetCubeSystem.Enable();
                break;
        }
    }

    IEnumerator BananaRoutine()
    {
        while (HasArtifact("KamikazeBanana"))
        {
            yield return new WaitForSeconds(15f);
            BananaSpawner.ThrowAtRandomEnemy(transform.position);
        }
    }
}
