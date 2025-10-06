using UnityEngine;

/// <summary>
/// StS-lignende håndlayout:
/// - Alle kort samme størrelse (handScale skalerer hele hånden).
/// - Symmetrisk bue: midten højest, kan vendes med Invert Arc.
/// - Overlap styres med span (cardStride/minSpan/maxSpan).
/// - Lav rotation for “crisp” højde på tværs.
/// </summary>
[ExecuteAlways]
public class CardHandArc : MonoBehaviour
{
    [Header("Global hand scale (gør hele hånden mindre/større)")]
    [SerializeField] private float handScale = 0.85f;

    [Header("Span/overlap (vandret udbredelse)")]
    [Tooltip("Vandret stride pr. mellemrum (brug (count-1) * stride). Lavere = mere overlap.")]
    [SerializeField] private float cardStride = 140f;
    [SerializeField] private float minSpan = 600f;
    [SerializeField] private float maxSpan = 1100f;

    [Header("Bueform")]
    [SerializeField] private float arcHeight = 140f;     // hvor meget midten løftes over siderne
    [SerializeField] private float verticalOffset = -120f;
    [SerializeField] private bool invertArc = false;     // vend buen (true = vender retningen)

    [Header("Rotation (lav = mere 'crisp')")]
    [SerializeField] private float maxRotation = 10f;

    [Header("Auto Update")]
    [SerializeField] private bool autoUpdateInPlayMode = true;
#if UNITY_EDITOR
    [SerializeField] private bool autoUpdateInEditor = true;
#endif

    private RectTransform[] cardSlots;

    private void OnEnable() { PositionCards(); }
    private void OnTransformChildrenChanged() { PositionCards(); }
    private void OnRectTransformDimensionsChange() { PositionCards(); }

#if UNITY_EDITOR
    private void Update()
    {
        if (!Application.isPlaying && autoUpdateInEditor) PositionCards();
        if (Application.isPlaying && autoUpdateInPlayMode) PositionCards();
    }
#endif

    [ContextMenu("Apply Now")]
    public void PositionCards()
    {
        int childCount = transform.childCount;
        if (childCount == 0) return;

        // KUN direkte børn er slots
        cardSlots = new RectTransform[childCount];
        for (int i = 0; i < childCount; i++)
            cardSlots[i] = transform.GetChild(i) as RectTransform;

        int count = cardSlots.Length;
        if (count == 0) return;

        // Ens skala for alle kort
        for (int i = 0; i < count; i++)
            cardSlots[i].localScale = Vector3.one * handScale;

        // Samlet span baseret på mellemrum (count - 1)
        float spanWanted = Mathf.Max(0, (count - 1)) * cardStride * handScale;
        float span = Mathf.Clamp(spanWanted, minSpan, maxSpan);

        // Z-orden: venstre nederst  højre øverst
        for (int i = 0; i < count; i++)
            cardSlots[i].SetSiblingIndex(i);

        // Læg jævnt langs [-span/2, +span/2]
        for (int i = 0; i < count; i++)
        {
            RectTransform slot = cardSlots[i];
            float t = (count > 1) ? (float)i / (count - 1) : 0.5f; // [0..1]

            // Vandret placering
            float x = Mathf.Lerp(-span * 0.5f, span * 0.5f, t);

            // *** SYMMETRISK BUE ***
            // sin(pi * t): 0 ved kanter (t=0/1), 1 i midten (t=0.5)
            float yArc = Mathf.Sin(Mathf.PI * t) * arcHeight;
            float y = (invertArc ? -yArc : +yArc) + verticalOffset;

            slot.anchoredPosition = new Vector2(x, y);

            // Rotation venstre(-)  højre(+), spejles ved invert for at “pege mod centrum”
            float rot = Mathf.Lerp(-maxRotation, +maxRotation, t);
            if (invertArc) rot = -rot;
            slot.localEulerAngles = new Vector3(0f, 0f, rot);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying) PositionCards();
    }
#endif
}
