using UnityEngine;

[CreateAssetMenu(fileName = "New Artifact", menuName = "Artifacts/Artifact")]
public class ArtifactData : ScriptableObject
{
    public string artifactName;
    [TextArea] public string description;
    public Sprite icon;

    public enum ArtifactRarity { Common, Uncommon, Rare, Epic, Legendary }
    public ArtifactRarity rarity;

    public string internalID; // bruges til at udløse effekt
}
