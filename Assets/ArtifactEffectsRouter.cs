// Scripts/Artifacts/ArtifactEffectsRouter.cs
using System.Linq;
using UnityEngine;

public class ArtifactEffectsRouter : MonoBehaviour
{
    public static ArtifactEffectsRouter I;

    [Header("Refs til artifacts (samme SO som i jeres katalog/inventory)")]
    public ScriptableObject charcoalSO;   // peg på jeres Charcoal artifact SO

    [Header("Procenter")]
    [Range(0f, 2f)] public float charcoalFireBonusPct = 0.25f; // fx +25%

    void Awake() { I = this; }

    /// Returnér samlet additiv skadebonus for given magitype (fx Fire).
    public float GetAdditiveDamagePct(MagicType magic)
    {
        // Har spilleren Charcoal?
        bool hasCharcoal =
            PlayerInventory.Instance != null &&
            PlayerInventory.Instance.artifacts != null &&
            PlayerInventory.Instance.artifacts.Contains(charcoalSO);

        float bonus = 0f;

        // Charcoal - kun Fire
        if (hasCharcoal && magic == MagicType.Fire)
            bonus += charcoalFireBonusPct;

        return bonus; // 0.25 = +25% (additiv)
    }
}
