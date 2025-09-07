using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardSlotUI : MonoBehaviour
{
    [Header("UI References (already assigned in prefab)")]
    [SerializeField] private Image cardArt;        // shows brown frame (filled) or blue empty (empty)
    [SerializeField] private Image iconImage;      // AbilityArt/Icon (circle image)
    [SerializeField] private TMP_Text nameText;    // AbilityName
    [SerializeField] private TMP_Text dmgText;     // DamageValue
    [SerializeField] private TMP_Text manaText;    // ManaValue
    [SerializeField] private Image rarityIcon;     // RarityIcon
    [SerializeField] private TMP_Text descriptionText; // Description (kept empty for hand)
    [SerializeField] private Image manaOverlay;    // ManaOverlay (grey-out)

    // Cached prefab frame sprite so we never need to wire it in Inspector
    private Sprite defaultFrameSprite;

    // Groups we toggle on/off so empty slot shows ONLY the blue board
    private GameObject goCardFrame;   // "CardFrame"
    private GameObject goAbilityArt;  // "AbilityArt"
    private GameObject goStatsRow;    // "StatsRow"
    private GameObject goDescription; // "Description"
    private GameObject goRarityIcon;  // "RarityIcon"

    private Ability currentAbility;

    private void Awake()
    {
        // cache default brown frame from prefab
        defaultFrameSprite = cardArt ? cardArt.sprite : null;

        // auto-find groups by name (matches your prefab hierarchy)
        goCardFrame = transform.Find("CardFrame")?.gameObject;
        goAbilityArt = transform.Find("AbilityArt")?.gameObject;
        goStatsRow = transform.Find("StatsRow")?.gameObject;
        goDescription = transform.Find("Description")?.gameObject;
        goRarityIcon = transform.Find("RarityIcon")?.gameObject;

        if (manaOverlay)
        {
            manaOverlay.enabled = false;
            manaOverlay.raycastTarget = false; // never block clicks
        }
        if (rarityIcon) rarityIcon.enabled = false;
    }

    public void Show(Ability a)
    {
        currentAbility = a;
        if (a == null) return;

        // --- show full card ---
        if (cardArt)
        {
            cardArt.sprite = defaultFrameSprite;   // back to brown frame
            cardArt.color = Color.white;
        }

        SetContentActive(true);

        // text & values
        if (nameText) nameText.text = a.abilityName;
        if (dmgText) dmgText.text = a.damage > 0 ? a.damage.ToString() : "—";
        if (manaText) manaText.text = a.manaCost.ToString("0");
        if (descriptionText) descriptionText.text = ""; // no description in hand

        // icon
        if (iconImage)
        {
            iconImage.sprite = a.icon;
            iconImage.color = a.icon ? Color.white : Color.clear;
            iconImage.preserveAspect = true;
        }

        // rarity
        var hand = FindFirstObjectByType<CardHandUI>();
        if (rarityIcon && hand != null)
        {
            rarityIcon.enabled = true;
            rarityIcon.sprite = hand.GetRarityIcon(a.rarity);
        }

        // hover payload
        var hover = GetComponent<CardHoverTrigger>();
        if (hover != null) hover.SetAbility(a);

        // mana overlay state will be updated from CardHandUI.UpdateCardOverlays()
        // via SetGreyedOut()
    }

    public void Clear()
    {
        currentAbility = null;

        var hand = FindFirstObjectByType<CardHandUI>();
        if (cardArt)
        {
            // show blue empty board; fallback to prefab frame if not set
            Sprite empty = hand ? hand.emptySlotSprite : null;
            cardArt.sprite = empty ? empty : defaultFrameSprite;
            cardArt.color = Color.white;
        }

        // hide ALL other visuals so only the empty board remains
        SetContentActive(false);

        // clean fields
        if (nameText) nameText.text = "";
        if (iconImage) { iconImage.sprite = null; iconImage.color = Color.clear; }
        if (dmgText) dmgText.text = "";
        if (manaText) manaText.text = "";
        if (descriptionText) descriptionText.text = "";
        if (rarityIcon) rarityIcon.enabled = false;

        // ensure overlay is off for empty slots
        if (manaOverlay) manaOverlay.enabled = false;

        // prevent hover from showing stale data
        var hover = GetComponent<CardHoverTrigger>();
        if (hover != null) hover.SetAbility(null);
    }

    public void SetGreyedOut(bool grey)
    {
        // Called by CardHandUI.UpdateCardOverlays()
        if (manaOverlay) manaOverlay.enabled = grey && currentAbility != null;
    }

    public Ability GetAbility() => currentAbility;

    // helper to toggle all content groups except CardArt itself
    private void SetContentActive(bool on)
    {
        if (goCardFrame) goCardFrame.SetActive(on);
        if (goAbilityArt) goAbilityArt.SetActive(on);
        if (goStatsRow) goStatsRow.SetActive(on);
        if (goDescription) goDescription.SetActive(on);
        if (goRarityIcon) goRarityIcon.SetActive(on);
    }
}
