using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MapDemo.Settings;

namespace MapDemo.Settings
{
    // Attach to prefabs or scene objects to apply visuals from MapSettings.
    // Works both in Play Mode and in Editor (ExecuteAlways).
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

        [Tooltip("Leave empty to use MapSettings.Current.")]
        public MapSettings settingsOverride;

        [Header("Node")]
        public MapNodeType nodeType = MapNodeType.Normal;
        public SpriteRenderer nodeBaseRenderer;
        public SpriteRenderer nodeRingRenderer;
        public Image nodeIconImage;
        public TextMeshProUGUI nodeLabelText;

        [Header("Edge")]
        public SpriteRenderer edgeLineRenderer;
        public SpriteRenderer edgeArrowRenderer;

        [Header("Label")]
        public TextMeshProUGUI labelText;
        public MapNodeType labelType = MapNodeType.Normal;

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
            // Re-apply immediately when something changes in the Inspector.
            ApplyNow();
        }
#endif

        public void ApplyNow()
        {
            var s = ResolveSettings();
            if (s == null) return;

            switch (mode)
            {
                case Mode.Node:
                    ApplyNode(s);
                    break;
                case Mode.Edge:
                    ApplyEdge(s);
                    break;
                case Mode.Label:
                    ApplyLabel(s);
                    break;
            }
        }

        private void ApplyNode(MapSettings s)
        {
            if (nodeBaseRenderer != null) nodeBaseRenderer.sprite = s.nodeBaseSprite;
            if (nodeRingRenderer != null) nodeRingRenderer.sprite = s.nodeRingSprite;

            if (s.TryGetStyle(nodeType, out var style))
            {
                // Tint the ring (or base) with the node color.
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