using UnityEngine;
using System;
using System.Collections.Generic;

// Dette script er lavet af Marc
namespace MapDemo.Settings
{
    /// <summary>
    /// MapSettings
    /// Ansvar: ScriptableObject der indeholder visuelle assets og per-encounter styling.
    /// - Indeholder globale sprites (node/edge) og en liste af NodeStyle for hver EncounterType.
    /// - Bygger et internt opslag (dictionary) ved OnEnable for hurtig opslag i runtime.
    /// </summary>
    [CreateAssetMenu(fileName = "MapSettings", menuName = "MapDemo/Map Settings", order = 10)]
    public sealed class MapSettings : ScriptableObject
    {
        [Header("Global Sprites")]
        /// <summary>Sprite som bruges som node-bund (baggrund) for alle noder.</summary>
        public Sprite nodeBaseSprite;
        /// <summary>Sprite som bruges til ring/outline omkring noder (kan farves).</summary>
        public Sprite nodeRingSprite;
        /// <summary>Sprite der bruges til kant-linjer mellem noder.</summary>
        public Sprite edgeLineSprite;
        /// <summary>Sprite der bruges til pil/arrow ved edge (hvis relevant).</summary>
        public Sprite edgeArrowSprite;

        /// <summary>
        /// NodeStyle
        /// Data container for styling pr. EncounterType:
        /// - type: encounter-typen (Normal, Elite, Boss ...)
        /// - label: tekst der kan vises i UI
        /// - color: farve der anvendes til ring eller base
        /// - icon: sprite der bruges som ikon i node UI
        /// </summary>
        [Serializable]
        public struct NodeStyle
        {
            public EncounterType type;
            public string label;
            public Color color;
            public Sprite icon;
        }

        [Header("Per-node styles")]
        /// <summary>Array af NodeStyle som kan konfigureres i Inspector.</summary>
        public NodeStyle[] nodeStyles = new NodeStyle[0];

        // Internt lookup for hurtige opslag: EncounterType -> NodeStyle
        private Dictionary<EncounterType, NodeStyle> _lookup;

        /// <summary>
        /// Unity callback: kaldt når ScriptableObject aktiveres / loades.
        /// Bygger lookup-dictionary så TryGetStyle kan være hurtig.
        /// </summary>
        private void OnEnable()
        {
            BuildLookup();
        }

        /// <summary>
        /// Byg eller genopbyg intern lookup-dictionary ud fra nodeStyles array.
        /// - Bevarer første forekomst for en given EncounterType.
        /// - Tager højde for null/empty arrays.
        /// </summary>
        private void BuildLookup()
        {
            if (_lookup == null) _lookup = new Dictionary<EncounterType, NodeStyle>();
            _lookup.Clear();
            if (nodeStyles == null) return;

            for (int i = 0; i < nodeStyles.Length; i++)
            {
                var ns = nodeStyles[i];
                if (!_lookup.ContainsKey(ns.type))
                    _lookup.Add(ns.type, ns);
            }
        }

        /// <summary>
        /// Forsøger at hente NodeStyle for en given EncounterType.
        /// - Returnerer true og out style hvis fundet.
        /// - Sørger for at lookup er bygget før opslag.
        /// </summary>
        public bool TryGetStyle(EncounterType type, out NodeStyle style)
        {
            if (_lookup == null || _lookup.Count == 0) BuildLookup();
            return _lookup.TryGetValue(type, out style);
        }

        // Cached reference så andre scripts kan tilgå et standard MapSettings asset.
        private static MapSettings _cached;
        /// <summary>
        /// Current
        /// Global adgang til et default MapSettings asset fra Resources/MapDemo/MapSettings_Default.
        /// - Hvis ikke fundet logges en advarsel (brug inspector eller opret asset).
        /// - Kan også sættes manuelt (bruges i tests eller runtime overrides).
        /// </summary>
        public static MapSettings Current
        {
            get
            {
                if (_cached == null)
                {
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
