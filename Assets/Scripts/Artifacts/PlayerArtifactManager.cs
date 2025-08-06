using System.Collections.Generic;
using UnityEngine;

public class PlayerArtifactManager : MonoBehaviour
{
    public static PlayerArtifactManager Instance;

    public List<ArtifactData> ownedArtifacts = new();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void AddArtifact(ArtifactData artifact)
    {
        if (!ownedArtifacts.Contains(artifact))
        {
            ownedArtifacts.Add(artifact);
            Debug.Log($"Artifact added: {artifact.artifactName}");

            ApplyArtifactEffect(artifact);
        }
    }

    void ApplyArtifactEffect(ArtifactData artifact)
    {
        switch (artifact.internalID)
        {
            case "DoubleGold":
                // fx. GameState.doubleGold = true;
                break;

            case "ShieldRegenBoost":
                // fx. øg shield regen i dit system
                break;

                // Tilføj flere cases
        }
    }

    public bool HasArtifact(string id)
    {
        return ownedArtifacts.Exists(a => a.internalID == id);
    }
}
