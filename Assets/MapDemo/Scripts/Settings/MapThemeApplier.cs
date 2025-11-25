using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MapDemo.Settings;

namespace MapDemo.Settings
{
    [ExecuteAlways]
    public sealed class MapThemeApplier : MonoBehaviour
    {
        public enum Mode
        {
            Node,
            Edge,
            Label
        }

        [Header("General")]
        public Mode mode = Mode.Node;
        public MapSettings settingsOverride;

        [Header("Node")]
        public EncounterType nodeType = EncounterType.Normal;   // <-- was MapNodeType
        public SpriteRenderer nodeBaseRenderer;
        public SpriteRenderer nodeRingRenderer;
        public Image nodeIconImage;
        public TextMeshProUGUI nodeLabelText;

        [Header("Edge")]
        public SpriteRenderer edgeLineRenderer;
        public SpriteRenderer edgeArrowRenderer;

        [Header("Label")]
        public TextMeshProUGUI labelText;
        public EncounterType labelType = EncounterType.Normal;  // <-- was MapNodeType

        private MapSettings ResolveSettings()
        {
            if (settingsOverride != null) return settingsOverride;
            return MapSettings.Current;
        }

        private void OnEnable()
        {
            ApplyNow();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ApplyNow();
        }
#endif

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

        private void ApplyNode(MapSettings s)
        {
            if (nodeBaseRenderer != null) nodeBaseRenderer.sprite = s.nodeBaseSprite;
            if (nodeRingRenderer != null) nodeRingRenderer.sprite = s.nodeRingSprite;

            if (s.TryGetStyle(nodeType, out var style))
            {
                if (nodeRingRenderer != null) nodeRingRenderer.color = style.color;
                else if (nodeBaseRenderer != null) nodeBaseRenderer.color = style.color;

                if (nodeIconImage != null) nodeIconImage.sprite = style.icon;
                if (nodeLabelText != null) nodeLabelText.text = style.label;
            }
        }

        private void ApplyEdge(MapSettings s)
        {
            if (edgeLineRenderer != null) edgeLineRenderer.sprite = s.edgeLineSprite;
            if (edgeArrowRenderer != null) edgeArrowRenderer.sprite = s.edgeArrowSprite;
        }

        private void ApplyLabel(MapSettings s)
        {
            if (s.TryGetStyle(labelType, out var style))
            {
                if (labelText != null) labelText.text = style.label;
            }
        }
    }
}
