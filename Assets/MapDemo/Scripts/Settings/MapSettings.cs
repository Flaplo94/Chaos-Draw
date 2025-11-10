using UnityEngine;
using System;
using System.Collections.Generic;


namespace MapDemo.Settings
{
    // Enum used by MapSettings. This does not need to match any existing project enum.
    // Treat it as a "theme key" for styling only.
    public enum MapNodeType
    {
        Start,
        Normal,
        Event,
        Shop,
        Elite,
        Special,
        Boss
    }

    [CreateAssetMenu(fileName = "MapSettings", menuName = "MapDemo/Map Settings", order = 10)]
    public sealed class MapSettings : ScriptableObject
    {
        [Header("Global Sprites")]
        public Sprite nodeBaseSprite;
        public Sprite nodeRingSprite;
        public Sprite edgeLineSprite;
        public Sprite edgeArrowSprite;

        [Serializable]
        public struct NodeStyle
        {
            public MapNodeType type;
            public string label;
            public Color color;
            public Sprite icon;
        }

        [Header("Per-node styles")]
        public NodeStyle[] nodeStyles = new NodeStyle[0];

        private Dictionary<MapNodeType, NodeStyle> _lookup;

        private void OnEnable()
        {
            BuildLookup();
        }

        private void BuildLookup()
        {
            if (_lookup == null) _lookup = new Dictionary<MapNodeType, NodeStyle>();
            _lookup.Clear();
            if (nodeStyles == null) return;
            for (int i = 0; i < nodeStyles.Length; i++)
            {
                var ns = nodeStyles[i];
                if (!_lookup.ContainsKey(ns.type))
                    _lookup.Add(ns.type, ns);
            }
        }

        public bool TryGetStyle(MapNodeType type, out NodeStyle style)
        {
            if (_lookup == null || _lookup.Count == 0) BuildLookup();
            return _lookup.TryGetValue(type, out style);
        }

        // Convenience: quick access to a shared instance via Resources.
        private static MapSettings _cached;
        public static MapSettings Current
        {
            get
            {
                if (_cached == null)
                {
                    // Looks for asset at: Resources/MapDemo/MapSettings_Default
                    _cached = Resources.Load<MapSettings>("MapDemo/MapSettings_Default");
                    if (_cached == null)
                    {
                        Debug.LogWarning("MapSettings.Current could not find Resources/MapDemo/MapSettings_Default. Assign manually or create the asset.");
                    }
                }
                return _cached;
            }
            set
            {
                _cached = value;
            }
        }
    }
}