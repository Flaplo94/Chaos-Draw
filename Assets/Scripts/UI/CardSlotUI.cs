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
        if (!faceFire) faceFire = transform.Find("Face_Fire") as RectTransform;
        if (!faceLightning) faceLightning = transform.Find("Face_Lightning") as RectTransform;
        if (!faceOther) faceOther = transform.Find("Face_Other") as RectTransform;
        if (!faceEmpty) faceEmpty = transform.Find("Face_Empty") as RectTransform;

        DeactivateAllFaces();
        if (faceOther) SetActive(faceOther);
        else if (faceEmpty) SetActive(faceEmpty);
    }

    public void Show(Ability a)
    {
        // Vælg korrekt face
        var next = a.magicType switch
        {
            MagicType.Fire => faceFire ? faceFire : faceOther,
            MagicType.Lightning => faceLightning ? faceLightning : faceOther,
            _ => faceOther
        };

        if (!next && faceEmpty) next = faceEmpty;
        SetActive(next);

        // Bind UI felter
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

        if (dmgValueText) dmgValueText.text = a.damage > 0 ? a.damage.ToString() : "—";
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
        if (faceEmpty) SetActive(faceEmpty);
        else DeactivateAllFaces();
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
