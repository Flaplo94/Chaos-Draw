using UnityEngine;
using UnityEngine.UI;

namespace ChaosDraw.SkillTree
{
    public class SkillTreeNode : MonoBehaviour
    {
        [Header("Data")]
        public NodeData data;

        [Header("UI")]
        public Image icon;        // selve ikonet (har også Button)
        public Image frame;       // ring
        public Image glowOuter;   // OuterGlow (farves fra NodeData)
        [Tooltip("Hvis tom: findes automatisk på icon eller roden.")]
        public Button button;

        [Header("State (runtime)")]
        public bool unlocked;
        public bool available;

        private static readonly Color LockedTint = new Color(0.4f, 0.4f, 0.4f, 1f);
        private static readonly Color AvailableTint = Color.white;

        private void Awake()
        {
            if (button == null && icon != null)
                button = icon.GetComponent<Button>();
            if (button == null)
                button = GetComponent<Button>();

            if (button != null)
                button.onClick.AddListener(OnClick);

            RefreshUI();
        }

        public void Bind(NodeData nodeData)
        {
            data = nodeData;

            if (icon != null)
                icon.sprite = data != null ? data.icon : null;

            if (frame != null && data != null && data.frame != null)
                frame.sprite = data.frame;

            RefreshUI();
        }

        public void SetAvailable(bool value)
        {
            available = value;
            RefreshUI();
        }

        public void ForceUnlock(bool value)
        {
            unlocked = value;
            RefreshUI();
        }

        public void Refresh() => RefreshUI();

        private void OnClick()
        {
            var mgr = GetComponentInParent<SkillTreeManager>();
            if (mgr != null && data != null)
                mgr.ShowNodeDetails(data);
        }

        private void RefreshUI()
        {
            // Ikon farves kun ift. locked/available; ingen custom tint fra data
            if (icon != null)
                icon.color = (unlocked || available) ? AvailableTint : LockedTint;

            // Outer glow kommer fra NodeData.glowColor, men alfa indikerer state
            // Outer glow: altid synlig; blot dæmpet når låst
            // Outer glow: altid synlig; dæmpet når låst/ikke tilgængelig
            if (glowOuter != null)
            {
                // Tweakbar i Inspector hvis du vil:
                const float ALPHA_LOCKED = 0.10f; // meget mørk = unavailable
                const float ALPHA_AVAILABLE = 0.55f; // kan købes
                const float ALPHA_UNLOCKED = 1.00f; // købt

                Color c = data != null ? data.glowColor : new Color(0.7f, 0.2f, 1f, 1f);
                c.a = unlocked ? ALPHA_UNLOCKED : (available ? ALPHA_AVAILABLE : ALPHA_LOCKED);
                glowOuter.color = c;
                glowOuter.enabled = true;
            }

            // (valgfrit) sørg for at frame aldrig bliver helt gennemsigtig
            if (frame != null)
            {
                var fc = frame.color;
                fc.a = Mathf.Max(fc.a, 0.35f);
                frame.color = fc;
                frame.enabled = true;
            }



            if (button != null)
                button.interactable = true; // panelet må altid åbnes
        }
    }
}
