using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// ONE prefab per slot. Inside the prefab, create 3 face parents:
///  - Face_Fire
///  - Face_Lightning
///  - Face_Other
/// Each face contains: Background (Image), AbilityName (TMP), RarityText (TMP), AbilityArt (Image)
/// The script toggles which face is active and binds text/icon.
public class CardSlotUI : MonoBehaviour
{
    [Header("Per-type face containers (parents)")]
    [SerializeField] private RectTransform faceFire;      // child named "Face_Fire"
    [SerializeField] private RectTransform faceLightning; // child named "Face_Lightning"
    [SerializeField] private RectTransform faceOther;     // child named "Face_Other"

    private RectTransform activeFace;

    private void Awake()
    {
        // Auto-find faces by name if not assigned
        if (!faceFire) faceFire = transform.Find("Face_Fire") as RectTransform;
        if (!faceLightning) faceLightning = transform.Find("Face_Lightning") as RectTransform;
        if (!faceOther) faceOther = transform.Find("Face_Other") as RectTransform;

        // Ensure exactly one face is active initially
        if (faceFire) faceFire.gameObject.SetActive(faceFire.gameObject.activeSelf);
        if (faceLightning) faceLightning.gameObject.SetActive(faceLightning.gameObject.activeSelf);
        if (faceOther) faceOther.gameObject.SetActive(faceOther.gameObject.activeSelf);

        // Fallback: if none active, default to Other
        if ((faceFire == null || !faceFire.gameObject.activeSelf) &&
            (faceLightning == null || !faceLightning.gameObject.activeSelf) &&
            (faceOther != null))
        {
            faceOther.gameObject.SetActive(true);
        }

        activeFace = (faceFire && faceFire.gameObject.activeSelf) ? faceFire
                   : (faceLightning && faceLightning.gameObject.activeSelf) ? faceLightning
                   : faceOther;
    }

    public void Show(Ability a)
    {
        SwitchToFace(a.magicType);

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

        // If this slot sits in a LayoutGroup, force a rebuild
        Canvas.ForceUpdateCanvases();
        var rt = GetComponent<RectTransform>();
        if (rt) LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
    }

    public void Clear()
    {
        if (!activeFace) return;

        var nameText = activeFace.Find("AbilityName")?.GetComponent<TextMeshProUGUI>();
        var rarityText = activeFace.Find("RarityText")?.GetComponent<TextMeshProUGUI>();
        var artImage = activeFace.Find("AbilityArt")?.GetComponent<Image>();

        if (nameText) nameText.text = "";
        if (rarityText) rarityText.text = "";
        if (artImage) { artImage.sprite = null; artImage.color = Color.clear; }
    }

    private void SwitchToFace(MagicType type)
    {
        if (faceFire) faceFire.gameObject.SetActive(false);
        if (faceLightning) faceLightning.gameObject.SetActive(false);
        if (faceOther) faceOther.gameObject.SetActive(false);

        activeFace = type switch
        {
            MagicType.Fire => faceFire ? faceFire : faceOther,
            MagicType.Lightning => faceLightning ? faceLightning : faceOther,
            _ => faceOther
        };

        if (activeFace) activeFace.gameObject.SetActive(true);
    }
}
