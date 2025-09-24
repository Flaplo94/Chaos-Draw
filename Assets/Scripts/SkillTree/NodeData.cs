using System.Collections.Generic;
using UnityEngine;

namespace ChaosDraw.SkillTree
{
    public enum NodeType
    {
        Meta,
        Fire,
        Lightning,
        Chaos
    }

    [CreateAssetMenu(fileName = "NodeData", menuName = "ChaosDraw/SkillTree/NodeData")]
    public class NodeData : ScriptableObject
    {
        [Header("Identity")]
        public string id;
        public string displayName;
        [TextArea] public string description;

        [Header("Visuals")]
        public NodeType nodeType;
        public Sprite icon;
        public Sprite frame;
        public Color glowColor = Color.magenta;

        [Header("Cost and Unlock")]
        public int cost = 1;                             // fallback pris pr. level
        public List<NodeData> prerequisites = new List<NodeData>();
        [Min(0)] public int minPrerequisites = 0;       // 0 = kræv alle i listen

        [Header("Levels (flat stats)")]
        [Min(1)] public int maxLevel = 3;               // fx 3
        // Hvad hver opgradering GIVER (inkrement). Eksempel: [2.5, 5, 10]
        public List<float> valuePerLevel = new List<float>() { 2.5f, 5f, 10f };
        // Valgfrit: pris pr. level. Tom => brug 'cost' hver gang.
        public List<int> costPerLevel = new List<int>();

        [Header("Effect (prototype)")]
        // Brug en enkel nøgle du kan summere på senere, fx "atk_dmg_pct", "atk_spd_pct"
        public string effectKey;
        // Kan efterlades tom – vi bruger valuePerLevel til flat stats
        public string effectValue;

        public bool HasPrerequisites()
        {
            return prerequisites != null && prerequisites.Count > 0;
        }
    }
}
