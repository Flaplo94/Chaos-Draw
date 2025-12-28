using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MapDemo.Settings;

// Dette script er lavet af Marc
namespace MapDemo.Settings
{
    /// <summary>
    /// MapThemeApplier
    /// Ansvar: Anvendelse af visuelle stilarter fra MapSettings på individuelle UI-elementer.
    /// - Kan sættes til at anvende stil for noder, kanter eller labels.
    /// - Understøtter et override MapSettings-objekt eller fallback til MapSettings.Current.
    /// Klassen er designet til at køre i editor (ExecuteAlways) så ændringer i Inspector reflekteres med det samme.
    /// </summary>
    [ExecuteAlways]
    public sealed class MapThemeApplier : MonoBehaviour
    {
        /// <summary>Hvilken type element denne instans anvender tema på.</summary>
        public enum Mode
        {
            Node,
            Edge,
            Label
        }

        [Header("General")]
        /// <summary>Vælg hvilken mode der skal anvendes (Node, Edge, Label).</summary>
        public Mode mode = Mode.Node;
        /// <summary>Valgfrit: lokal settings override. Hvis null bruges MapSettings.Current.</summary>
        public MapSettings settingsOverride;

        [Header("Node")]
        /// <summary>Typen af node der styles (bruges når mode == Node).</summary>
        public EncounterType nodeType = EncounterType.Normal;
        /// <summary>SpriteRenderer der repræsenterer node-basen (baggrund).</summary>
        public SpriteRenderer nodeBaseRenderer;
        /// <summary>SpriteRenderer der repræsenterer node-ringen (kan farves).</summary>
        public SpriteRenderer nodeRingRenderer;
        /// <summary>UI Image til node-ikon (valgfri, bruges hvis style indeholder icon).</summary>
        public Image nodeIconImage;
        /// <summary>Tekstfelt for node-label (valgfri).</summary>
        public TextMeshProUGUI nodeLabelText;

        [Header("Edge")]
        /// <summary>SpriteRenderer for kantens linje (edge line).</summary>
        public SpriteRenderer edgeLineRenderer;
        /// <summary>SpriteRenderer for kant-peg (pil) hvis relevant.</summary>
        public SpriteRenderer edgeArrowRenderer;

        [Header("Label")]
        /// <summary>Label tekst-komponent (bruges når mode == Label).</summary>
        public TextMeshProUGUI labelText;
        /// <summary>EncounterType der afgør hvilken label tekst der skal anvendes.</summary>
        public EncounterType labelType = EncounterType.Normal;

        /// <summary>
        /// ResolveSettings
        /// Returnerer settingsOverride hvis sat, ellers MapSettings.Current.
        /// </summary>
        private MapSettings ResolveSettings()
        {
            if (settingsOverride != null) return settingsOverride;
            return MapSettings.Current;
        }

        /// <summary>
        /// OnEnable
        /// Sørger for at anvende temaet ved aktivering (både i editor og runtime).
        /// </summary>
        private void OnEnable()
        {
            ApplyNow();
        }

#if UNITY_EDITOR
        /// <summary>
        /// OnValidate
        /// Kører i editor når værdier i Inspector ændres — anvender tema straks.
        /// Kun kompileret i editor builds.
        /// </summary>
        private void OnValidate()
        {
            ApplyNow();
        }
#endif

        /// <summary>
        /// ApplyNow
        /// Offentlig helper til straks at anvende valgte stilelementer.
        /// </summary>
        public void ApplyNow()
        {
            var s = ResolveSettings();
            if (s == null) return;

            switch (mode)
            {
                case Mode.Node: ApplyNode(s); break;
                case Mode.Edge: ApplyEdge(s); break;
                case Mode.Label: ApplyLabel(s); break;
            }
        }

        /// <summary>
        /// ApplyNode
        /// Anvender sprites og farver for en node baseret på MapSettings og valgt EncounterType.
        /// - Først sætter sprites fra settings (base + ring).
        /// - Dernæst anvendes style (farve, icon, label) hvis TryGetStyle returnerer et style.
        /// </summary>
        private void ApplyNode(MapSettings s)
        {
            // Sæt base sprites hvis renderer findes
            if (nodeBaseRenderer != null) nodeBaseRenderer.sprite = s.nodeBaseSprite;
            if (nodeRingRenderer != null) nodeRingRenderer.sprite = s.nodeRingSprite;

            // Hent stil for den givne nodeType og anvend farve / icon / label
            if (s.TryGetStyle(nodeType, out var style))
            {
                // Prioriter ring-farve hvis ring renderer findes, ellers farv basen
                if (nodeRingRenderer != null) nodeRingRenderer.color = style.color;
                else if (nodeBaseRenderer != null) nodeBaseRenderer.color = style.color;

                if (nodeIconImage != null) nodeIconImage.sprite = style.icon;
                if (nodeLabelText != null) nodeLabelText.text = style.label;
            }
        }

        /// <summary>
        /// ApplyEdge
        /// Anvender sprites for kant-linje og kant-pil fra settings.
        /// </summary>
        private void ApplyEdge(MapSettings s)
        {
            if (edgeLineRenderer != null) edgeLineRenderer.sprite = s.edgeLineSprite;
            if (edgeArrowRenderer != null) edgeArrowRenderer.sprite = s.edgeArrowSprite;
        }

        /// <summary>
        /// ApplyLabel
        /// Anvender label-tekst baseret på encounter-typen via MapSettings.
        /// </summary>
        private void ApplyLabel(MapSettings s)
        {
            if (s.TryGetStyle(labelType, out var style))
            {
                if (labelText != null) labelText.text = style.label;
            }
        }
    }
}
