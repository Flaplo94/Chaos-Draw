using UnityEngine;

[ExecuteAlways]
public class CardHandArc : MonoBehaviour
{
    [Header("Slots (assign in Inspector)")]
    [SerializeField] private RectTransform[] cardSlots;

    [Header("Arc Layout")]
    [SerializeField] private float radius = 360f;
    [SerializeField] private float angleDelta = 9f;           // base fan angle step
    [SerializeField] private float verticalOffset = -40f;     // overall hand offset

    // Hvordan Y-højden for det valgte kort bestemmes
    public enum SelectedYMode { AdditiveLift, FlatScreenHeight }

    [Header("Selected Lift / Height")]
    [SerializeField] private int selectedIndex = -1;          // index fra UI
    [SerializeField] private SelectedYMode selectedYMode = SelectedYMode.FlatScreenHeight;

    // AdditiveLift: brug en relativ løfteværdi (som før)
    [SerializeField] private float selectedLiftY = 120f;

    // FlatScreenHeight: lås valgt kort til (maxBaseY + flatLiftY)
    [SerializeField] private float flatLiftY = 120f;

    [SerializeField] private float selectedScale = 1.12f;
    [SerializeField] private bool bringSelectedToFront = true;

    [Header("Neighbor Push (space for selected)")]
    [SerializeField] private float neighborPushX = 40f;       // +/- X for naboer
    [SerializeField] private float neighborAngleRelax = 4f;   // reducer vinkler lidt omkring selected

    [Header("Index Mapping (fix off-by-one / reversed order)")]
    [SerializeField] private bool invertIndexOrder = false;   // spejlvend rækkefølge
    [SerializeField] private int indexOffset = 0;             // konsistent +1 / -1 hvis nødvendigt

    [Header("Runtime/Editor")]
    [SerializeField] private bool autoUpdateInPlayMode = true;
    [SerializeField] private bool autoUpdateInEditor = true;

    void OnEnable() { Rebuild(); }
    void OnValidate() { Rebuild(); }

    void Update()
    {
        if (Application.isPlaying)
        {
            if (autoUpdateInPlayMode) Rebuild();
        }
        else
        {
            if (autoUpdateInEditor) Rebuild();
        }
    }

    public void SetSelectedIndex(int index)
    {
        selectedIndex = index;
        Rebuild();
    }

    public int GetSelectedIndex() => selectedIndex;

    public void SetSlots(RectTransform[] slots)
    {
        cardSlots = slots;
        Rebuild();
    }

    // Map fra UI-index til slot-index (hvis rækkefølgen er spejlvendt eller off-by-one)
    private int MapIndex(int rawIndex)
    {
        if (cardSlots == null || cardSlots.Length == 0) return -1;
        if (rawIndex < 0) return -1;

        int n = cardSlots.Length;
        int i = rawIndex;

        if (invertIndexOrder)
            i = (n - 1) - i;

        i += indexOffset;
        if (i < 0 || i >= n) return -1;

        return i;
    }

    private void Rebuild()
    {
        if (cardSlots == null || cardSlots.Length == 0) return;

        int n = cardSlots.Length;
        float midpoint = (n - 1) * 0.5f;

        // 1) Beregn basis-positioner/rotationer for hele buen først (så vi kender maxY)
        Vector2[] basePos = new Vector2[n];
        float[] baseAngleZ = new float[n];
        float maxBaseY = float.NegativeInfinity;

        for (int i = 0; i < n; i++)
        {
            var slot = cardSlots[i];
            if (!slot) continue;

            float angle = angleDelta * (midpoint - i);  // venstre positiv, højre negativ
            float angleRad = -angle * Mathf.Deg2Rad;    // inverteret for trig

            float x = Mathf.Sin(angleRad) * radius;
            float y = Mathf.Cos(angleRad) * radius;

            Vector2 pos = new Vector2(x, y + verticalOffset);
            basePos[i] = pos;
            baseAngleZ[i] = angle;

            if (y + verticalOffset > maxBaseY)
                maxBaseY = y + verticalOffset;
        }

        // 2) Skriv basislayout ind (pos/rot/scale)
        for (int i = 0; i < n; i++)
        {
            var slot = cardSlots[i];
            if (!slot) continue;

            slot.anchoredPosition = basePos[i];
            slot.localRotation = Quaternion.Euler(0f, 0f, baseAngleZ[i]);
            slot.localScale = Vector3.one;
        }

        // 3) Selected-behandling + naboer
        int iSel = MapIndex(selectedIndex);
        if (iSel >= 0 && iSel < n && cardSlots[iSel] != null)
        {
            var sel = cardSlots[iSel];

            // 0° rotation + skalering
            sel.localRotation = Quaternion.identity;
            sel.localScale = Vector3.one * selectedScale;

            // Y-højde for valgt kort
            Vector2 p = sel.anchoredPosition;
            if (selectedYMode == SelectedYMode.FlatScreenHeight)
            {
                // Lås Y til "samme højde for alle" = håndens højeste baseY + flatLiftY
                p.y = maxBaseY + flatLiftY;
            }
            else // AdditiveLift
            {
                // Klassisk: læg et relativt løft ovenpå basispositionen
                p.y = basePos[iSel].y + selectedLiftY;
            }
            sel.anchoredPosition = p;

            // Bring forrest i hierarkiet (så det aldrig bliver dækket)
            if (bringSelectedToFront) sel.SetAsLastSibling();

            // Skub naboer og "slap" deres vinkel en anelse
            for (int i = 0; i < n; i++)
            {
                if (i == iSel) continue;
                var slot = cardSlots[i];
                if (!slot) continue;

                // X-skub
                float dir = Mathf.Sign(i - iSel); // venstre: -1, højre: +1
                var np = slot.anchoredPosition;
                np.x = basePos[i].x + dir * neighborPushX;
                slot.anchoredPosition = np;

                // Reducer absolut vinkel
                float currentZ = baseAngleZ[i];
                float signedZ = currentZ; // vi lagde den som [-, +] i baseAngleZ allerede
                float reduced = Mathf.Sign(signedZ) * Mathf.Max(0f, Mathf.Abs(signedZ) - neighborAngleRelax);
                slot.localRotation = Quaternion.Euler(0f, 0f, reduced);
            }
        }
    }
}
