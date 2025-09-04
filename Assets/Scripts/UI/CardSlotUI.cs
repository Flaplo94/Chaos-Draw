using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardSlotUI : MonoBehaviour
{
    [Header("Per-type face containers (parents)")]
    [SerializeField] private RectTransform faceFire;       // child: "Face_Fire"
    [SerializeField] private RectTransform faceLightning;  // child: "Face_Lightning"
    [SerializeField] private RectTransform faceOther;      // child: "Face_Other"
    [SerializeField] private RectTransform faceEmpty;      // child: "Face_Empty"

    [Header("Overlay Images (per face)")]
    [SerializeField] private Image overlayFire;       // child inside Face_Fire
    [SerializeField] private Image overlayLightning;  // child inside Face_Lightning
    [SerializeField] private Image overlayOther;      // child inside Face_Other

    private RectTransform activeFace;
    private Image activeOverlay;
    private Ability currentAbility;

    private void Awake()
    {
        if (!faceFire) faceFire = transform.Find("Face_Fire") as RectTransform;
        if (!faceLightning) faceLightning = transform.Find("Face_Lightning") as RectTransform;
        if (!faceOther) faceOther = transform.Find("Face_Other") as RectTransform;
        if (!faceEmpty) faceEmpty = transform.Find("Face_Empty") as RectTransform;

        DeactivateAllFaces();

        if (faceEmpty) SetActive(faceEmpty);
        else if (faceOther) SetActive(faceOther);

        if (activeOverlay != null)
            activeOverlay.enabled = false;
    }

    public void Show(Ability a)
    {
        currentAbility = a;

        // pick correct face
        var next = a.magicType switch
        {
            MagicType.Fire => faceFire ? faceFire : faceOther,
            MagicType.Lightning => faceLightning ? faceLightning : faceOther,
            _ => faceOther
        };

        if (!next && faceEmpty) next = faceEmpty;
        SetActive(next);

        // Bind UI
        var nameText = activeFace.Find("AbilityName")?.GetComponent<TextMeshProUGUI>();
        var rarityText = activeFace.Find("AbilityRarity")?.GetComponent<TextMeshProUGUI>();
        var artImage = activeFace.Find("AbilityArt")?.GetComponent<Image>();
        var dmgValueText = activeFace.Find("StatsRow/DamageIcon/DamageValue")?.GetComponent<TextMeshProUGUI>();
        var manaValueText = activeFace.Find("StatsRow/ManaIcon/ManaValue")?.GetComponent<TextMeshProUGUI>();

        if (nameText) nameText.text = a.abilityName;
        if (rarityText) rarityText.text = a.rarity.ToString();

        if (artImage)
        {
            artImage.sprite = a.icon;
            artImage.color = a.icon ? Color.white : Color.clear;
            artImage.preserveAspect = true;
        }

        if (dmgValueText) dmgValueText.text = a.damage > 0 ? a.damage.ToString() : "�";
        if (manaValueText) manaValueText.text = a.manaCost.ToString("0");

        //  Hover support
        var hover = activeFace.GetComponent<CardHoverTrigger>();
        if (hover != null)
        {
            hover.SetAbility(a);
            Debug.Log("[CardSlotUI] Hover ability sat: " + a.abilityName);
        }

        ForceLayout();
    }

    public void Clear()
    {
        currentAbility = null;
        if (faceEmpty) SetActive(faceEmpty);
        else DeactivateAllFaces();

        if (activeOverlay != null)
            activeOverlay.enabled = false;
    }

    public void SetGreyedOut(bool grey)
    {
        if (activeOverlay != null)
            activeOverlay.enabled = grey;
    }

    public Ability GetAbility() => currentAbility;

    private void SetActive(RectTransform face)
    {
        DeactivateAllFaces();
        activeFace = face;
        if (activeFace) activeFace.gameObject.SetActive(true);

        // pick the right overlay for this face
        if (face == faceFire) activeOverlay = overlayFire;
        else if (face == faceLightning) activeOverlay = overlayLightning;
        else if (face == faceOther) activeOverlay = overlayOther;

        if (activeOverlay != null)
            activeOverlay.enabled = false;
    }

    private void DeactivateAllFaces()
    {
        if (faceFire) faceFire.gameObject.SetActive(false);
        if (faceLightning) faceLightning.gameObject.SetActive(false);
        if (faceOther) faceOther.gameObject.SetActive(false);
        if (faceEmpty) faceEmpty.gameObject.SetActive(false);
        activeFace = null;
        activeOverlay = null;
    }

    private void ForceLayout()
    {
        Canvas.ForceUpdateCanvases();
        var rt = GetComponent<RectTransform>();
        if (rt) LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
    }
}
