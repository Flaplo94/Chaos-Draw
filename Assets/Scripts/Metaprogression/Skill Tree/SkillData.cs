using UnityEngine;

public enum SkillType { StatBoost, Unlock, Utility }

[CreateAssetMenu(menuName = "ChaosDraw/Skill")]
public class SkillData : ScriptableObject
{
    public string internalID;             // Unikt ID fx "DamageUp1"
    public string displayName;            // Navnet der vises i UI
    [TextArea] public string description; // Beskrivelse til popup
    public Sprite icon;                   // Ikon i træet
    public SkillType type;                // Hvilken slags skill

    [Header("Requirements")]
    public SkillData[] prerequisites;     // Krævede andre noder
    public int cost = 1;                  // Skill point pris

    [Header("Effects")]
    public float value;                   // Fx +10% damage
    public string unlockKey;              // Fx "LegacyDeck"
}
