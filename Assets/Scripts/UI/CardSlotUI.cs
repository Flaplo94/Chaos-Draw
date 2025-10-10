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

    [Header("Mana Overlay")]
    [SerializeField] private Image manaOverlay;    // ManaOverlay (grey-out)
    [SerializeField, Range(0f, 1f)] private float overlayAlpha = 0.6f;

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

        // --- Robust overlay binding ---
        if (!manaOverlay)
            manaOverlay = transform.Find("ManaOverlay")?.GetComponent<Image>();

        if (manaOverlay)
        {
            // make sure it's visible when enabled
            var c = manaOverlay.color;
            c.a = overlayAlpha;
            manaOverlay.color = c;

            // ensure it's above everything else
            manaOverlay.transform.SetAsLastSibling();

            manaOverlay.enabled = false;       // start hidden
            manaOverlay.raycastTarget = false; // never block clicks
        }

        if (rarityIcon) rarityIcon.enabled = false;
    }

    public void Show(Ability a)
    {
        currentAbility = a;
        if (a == null) return;

        // turn on the visible card background/frame
        if (cardArt)
        {
            cardArt.enabled = true;
            cardArt.sprite = defaultFrameSprite; // your brown frame sprite
            cardArt.color = Color.white;
        }

        SetContentActive(true);

        if (nameText) nameText.text = a.abilityName;
        if (dmgText) dmgText.text = a.damage > 0 ? a.damage.ToString() : "—";
        if (manaText) manaText.text = a.manaCost.ToString("0");
        if (descriptionText) descriptionText.text = "";

        if (iconImage)
        {
            iconImage.sprite = a.icon;
            iconImage.color = a.icon ? Color.white : Color.clear;
            iconImage.preserveAspect = true;
        }

        var hand = FindFirstObjectByType<CardHandUI>();
        if (rarityIcon && hand != null)
        {
            rarityIcon.enabled = true;
            rarityIcon.sprite = hand.GetRarityIcon(a.rarity);
        }

        var hover = GetComponent<CardHoverTrigger>();
        if (hover != null) hover.SetAbility(a);

        // keep overlay on top after Show()
        if (manaOverlay) manaOverlay.transform.SetAsLastSibling();
    }

    public void Clear()
    {
        currentAbility = null;

        // turn the frame fully off (no permanent slot)
        if (cardArt)
        {
            cardArt.enabled = false;
            cardArt.sprite = defaultFrameSprite;
            cardArt.color = Color.white;
        }

        // hide all card elements
        SetContentActive(false);

        // clear fields
        if (nameText) nameText.text = "";
        if (iconImage) { iconImage.sprite = null; iconImage.color = Color.clear; }
        if (dmgText) dmgText.text = "";
        if (manaText) manaText.text = "";
        if (descriptionText) descriptionText.text = "";
        if (rarityIcon) rarityIcon.enabled = false;
        if (manaOverlay) manaOverlay.enabled = false;

        var hover = GetComponent<CardHoverTrigger>();
        if (hover != null) hover.SetAbility(null);
    }

    public void SetGreyedOut(bool grey)
    {
        // Called by CardHandUI.UpdateCardOverlays()
        if (!manaOverlay || currentAbility == null)
            return;

        // keep overlay above siblings
        manaOverlay.transform.SetAsLastSibling();

        // ensure alpha in case prefab color was reset
        var c = manaOverlay.color;
        c.a = overlayAlpha;
        manaOverlay.color = c;

        manaOverlay.enabled = grey;
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
