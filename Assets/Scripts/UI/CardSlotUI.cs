using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardSlotUI : MonoBehaviour
{
    [Header("Per-type face containers (parents)")]
    [SerializeField] private RectTransform faceFire;       // child: "Face_Fire"
    [SerializeField] private RectTransform faceLightning;  // child: "Face_Lightning"
    [SerializeField] private RectTransform faceOther;      // child: "Face_Other"
    [SerializeField] private RectTransform faceEmpty;      // child: "Face_Empty" (no visuals)

    private RectTransform activeFace;

    private void Awake()
    {
        // Auto-find faces by name if not assigned
        if (!faceFire) faceFire = transform.Find("Face_Fire") as RectTransform;
        if (!faceLightning) faceLightning = transform.Find("Face_Lightning") as RectTransform;
        if (!faceOther) faceOther = transform.Find("Face_Other") as RectTransform;
        if (!faceEmpty) faceEmpty = transform.Find("Face_Empty") as RectTransform;

        // Ensure exactly one face is active initially (default to empty if nothing is active)
        DeactivateAllFaces();
        if (faceOther) SetActive(faceOther); // default prefab look
        else if (faceEmpty) SetActive(faceEmpty);
    }

    public void Show(Ability a)
    {
        // Switch to the right type face
        var next = a.magicType switch
        {
            MagicType.Fire => faceFire ? faceFire : faceOther,
            MagicType.Lightning => faceLightning ? faceLightning : faceOther,
            _ => faceOther
        };

        if (!next && faceEmpty) next = faceEmpty; // absolute fallback
        SetActive(next);

        // Bind inside ACTIVE face
        var nameText = activeFace.Find("AbilityName")?.GetComponent<TextMeshProUGUI>();
        var rarityText = activeFace.Find("RarityText")?.GetComponent<TextMeshProUGUI>();
        var artImage = activeFace.Find("AbilityArt")?.GetComponent<Image>();

        if (nameText) nameText.text = a.abilityName;
        if (rarityText) rarityText.text = a.rarity.ToString();

        if (artImage)
        {
            artImage.sprite = a.icon;
            artImage.color = a.icon ? Color.white : Color.clear;
            artImage.preserveAspect = true;
        }

        ForceLayout();
    }

    public void Clear()
    {
        // Show the empty face (no frame), or hide all faces if you didn't add one
        if (faceEmpty) SetActive(faceEmpty);
        else DeactivateAllFaces(); // this will show nothing; slot root stays in layout
    }

    private void SetActive(RectTransform face)
    {
        if (face == activeFace) return;

        DeactivateAllFaces();
        activeFace = face;
        if (activeFace) activeFace.gameObject.SetActive(true);
    }

    private void DeactivateAllFaces()
    {
        if (faceFire) faceFire.gameObject.SetActive(false);
        if (faceLightning) faceLightning.gameObject.SetActive(false);
        if (faceOther) faceOther.gameObject.SetActive(false);
        if (faceEmpty) faceEmpty.gameObject.SetActive(false);
        activeFace = null;
    }

    private void ForceLayout()
    {
        Canvas.ForceUpdateCanvases();
        var rt = GetComponent<RectTransform>();
        if (rt) LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
    }
}
