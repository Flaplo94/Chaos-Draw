using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class CardHandUI : MonoBehaviour
{
    [Header("Hand (static children under parent)")]
    [SerializeField] private Transform handParent;     // Put EXACTLY 4 CardSlot instances under this
    [SerializeField] private CardSlotUI[] cardSlots;   // Leave empty in Inspector; auto-filled at runtime

    [Header("Counters")]
    [SerializeField] private TextMeshProUGUI discardCounterText;
    [SerializeField] private TextMeshProUGUI deckCounterText;

    [Header("Pile UI (face-down stacks)")]
    [SerializeField] private DeckSlotUI drawPileUI;
    [SerializeField] private DeckSlotUI discardPileUI;

    [Header("Card Backs (per magic type)")]
    [SerializeField] private Sprite fireBack;
    [SerializeField] private Sprite lightningBack;
    [SerializeField] private Sprite otherBack;
    [SerializeField] private Sprite emptyBack;

    [Header("Card Pool")]
    [SerializeField] private List<StartingCard> startingDeckList = new();
    private List<Ability> allAbilities = new();

    [Header("Reward UI (parent + per-type prefabs)")]
    [SerializeField] private GameObject rewardUI;
    [SerializeField] private Transform rewardCardsParent; // Layout parent for rewards
    [SerializeField] private GameObject fireRewardCardPrefab;
    [SerializeField] private GameObject lightningRewardCardPrefab;
    [SerializeField] private GameObject otherRewardCardPrefab;
    [SerializeField] private Button skipButton;

    private int waveCount = 0;
    private readonly List<Ability> deck = new();
    private readonly List<Ability> drawPile = new();
    private readonly List<Ability> discardPile = new();
    private Ability[] hand; // sized to cardSlots.Length at runtime

    [System.Serializable]
    public class StartingCard
    {
        public string abilityName;
        public int count;
    }

    private void Start()
    {
        // --- Auto-wire: find CardSlotUI components under the parent (no instantiation) ---
        if (!handParent)
        {
            Debug.LogError("CardHandUI: handParent is not assigned.");
            return;
        }

        cardSlots = handParent.GetComponentsInChildren<CardSlotUI>(true);
        if (cardSlots == null || cardSlots.Length == 0)
        {
            Debug.LogError("CardHandUI: No CardSlotUI children found under handParent. Place exactly 4 CardSlot prefab instances there.");
            return;
        }

        hand = new Ability[cardSlots.Length];

        // --- Load abilities / deck / draw ---
        allAbilities = new List<Ability>(Resources.LoadAll<Ability>(""));
        CreateStartingDeck();
        Shuffle(drawPile);

        UpdateDiscardText();
        UpdateDeckText();
        UpdatePileUIs();

        DrawInitialHand();
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame && hand.Length > 0) TryUseCard(0);
        if (Keyboard.current.digit2Key.wasPressedThisFrame && hand.Length > 1) TryUseCard(1);
        if (Keyboard.current.digit3Key.wasPressedThisFrame && hand.Length > 2) TryUseCard(2);
        if (Keyboard.current.digit4Key.wasPressedThisFrame && hand.Length > 3) TryUseCard(3);
    }

    // -------------------- Deck / Draw / Use --------------------
    private void CreateStartingDeck()
    {
        deck.Clear();
        drawPile.Clear();
        discardPile.Clear();

        UpdateDiscardText();

        foreach (var entry in startingDeckList)
        {
            Ability match = allAbilities.Find(a => a.name == entry.abilityName);
            if (match == null)
            {
                Debug.LogWarning("Ability not found: " + entry.abilityName);
                continue;
            }

            for (int i = 0; i < entry.count; i++)
                deck.Add(match);
        }

        drawPile.AddRange(deck);

        UpdateDeckText();
        UpdatePileUIs();
    }

    private void DrawInitialHand()
    {
        for (int i = 0; i < hand.Length; i++)
            DrawCard(i);
    }

    private void DrawCard(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= hand.Length) return;

        // If draw pile is empty but all cards are in discard, reshuffle discard -> draw
        if (drawPile.Count == 0 && discardPile.Count == deck.Count)
        {
            drawPile.AddRange(discardPile);
            discardPile.Clear();
            Shuffle(drawPile);

            UpdateDiscardText();
            UpdateDeckText();
            UpdatePileUIs();

            // Fill empty hand slots now that we have a fresh draw pile
            for (int i = 0; i < hand.Length; i++)
                if (hand[i] == null)
                    DrawCard(i);
            return;
        }

        if (drawPile.Count == 0)
        {
            UpdateDeckText();
            UpdatePileUIs(); // (will show emptyBack if provided)
            return;
        }

        Ability card = drawPile[0];
        drawPile.RemoveAt(0);
        UpdateDeckText();
        UpdatePileUIs(); // top back may change after removing

        hand[slotIndex] = card;

        // Bind into the existing slot (slot toggles the correct face internally)
        if (cardSlots != null && slotIndex < cardSlots.Length && cardSlots[slotIndex] != null)
            cardSlots[slotIndex].Show(card);
    }

    private void TryUseCard(int index)
    {
        if (hand == null || index < 0 || index >= hand.Length) return;
        if (hand[index] == null) return;

        bool success = hand[index].Activate();
        if (!success) return;

        discardPile.Add(hand[index]);
        hand[index] = null;

        if (cardSlots != null && index < cardSlots.Length && cardSlots[index] != null)
            cardSlots[index].Clear();

        UpdateDiscardText();
        UpdatePileUIs();

        DrawCard(index);
    }

    // -------------------- UI counters / pile backs --------------------
    private void UpdateDiscardText()
    {
        if (discardCounterText != null)
            discardCounterText.text = discardPile.Count.ToString();
    }

    private void UpdateDeckText()
    {
        if (deckCounterText != null)
            deckCounterText.text = drawPile.Count.ToString();
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int rand = Random.Range(i, list.Count);
            (list[i], list[rand]) = (list[rand], list[i]);
        }
    }

    public void OnWaveCompleted()
    {
        waveCount++;
        if (waveCount % 4 == 0)
            ShowRewardUI();
    }

    private Sprite GetBackFor(MagicType type)
    {
        return type switch
        {
            MagicType.Fire => fireBack,
            MagicType.Lightning => lightningBack,
            _ => otherBack
        };
    }

    private Sprite GetTopBackFromPile(List<Ability> pile)
    {
        if (pile == null || pile.Count == 0)
            return emptyBack ? emptyBack : otherBack;

        // Draw pile top = index 0; Discard pile "top" = last added = last index
        var top = ReferenceEquals(pile, discardPile)
            ? pile[pile.Count - 1]
            : pile[0];

        return GetBackFor(top.magicType);
    }

    private void UpdatePileUIs()
    {
        if (drawPileUI != null)
            drawPileUI.Set(GetTopBackFromPile(drawPile), drawPile.Count);

        if (discardPileUI != null)
            discardPileUI.Set(GetTopBackFromPile(discardPile), discardPile.Count);
    }

    // -------------------- Rewards (per-type prefabs under parent) --------------------
    private GameObject GetRewardCardPrefab(MagicType type)
    {
        switch (type)
        {
            case MagicType.Fire: return fireRewardCardPrefab ? fireRewardCardPrefab : otherRewardCardPrefab;
            case MagicType.Lightning: return lightningRewardCardPrefab ? lightningRewardCardPrefab : otherRewardCardPrefab;
            default: return otherRewardCardPrefab;
        }
    }

    private void ClearRewardCardsParent()
    {
        if (!rewardCardsParent) return;
        for (int i = rewardCardsParent.childCount - 1; i >= 0; i--)
            Destroy(rewardCardsParent.GetChild(i).gameObject);
    }

    private void BuildRewardCard(Ability ability)
    {
        if (!rewardCardsParent)
        {
            Debug.LogError("CardHandUI: rewardCardsParent is not assigned.");
            return;
        }

        var prefab = GetRewardCardPrefab(ability.magicType);
        var cardGO = Instantiate(prefab, rewardCardsParent);

        // Look up parts by name (make sure your reward prefabs use these names)
        var nameText = cardGO.transform.Find("AbilityName")?.GetComponent<TextMeshProUGUI>();
        var artImage = cardGO.transform.Find("AbilityArt")?.GetComponent<Image>();
        var descText = cardGO.transform.Find("AbilityDescription")?.GetComponent<TextMeshProUGUI>();
        var rarityTxt = cardGO.transform.Find("RarityText")?.GetComponent<TextMeshProUGUI>();
        var button = cardGO.GetComponent<Button>();

        if (nameText) nameText.text = ability.abilityName;
        if (artImage) { artImage.sprite = ability.icon; artImage.color = Color.white; artImage.preserveAspect = true; }
        if (descText) descText.text = ability.description;
        if (rarityTxt) rarityTxt.text = ability.rarity.ToString();

        if (button)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                AddCardToDeck(ability);
                CloseRewardUI();
            });
        }
    }

    private void ShowRewardUI()
    {
        Time.timeScale = 0f;
        rewardUI.SetActive(true);
        ClearRewardCardsParent();

        List<Ability> pool = new List<Ability>(allAbilities);
        Shuffle(pool);

        for (int i = 0; i < 3 && i < pool.Count; i++)
        {
            Ability abilityCopy = Instantiate(pool[i]); // avoid mutating base asset
            abilityCopy.rarity = RollRarity();
            BuildRewardCard(abilityCopy);
        }

        if (skipButton != null)
        {
            skipButton.onClick.RemoveAllListeners();
            skipButton.onClick.AddListener(CloseRewardUI);
        }
    }

    private void CloseRewardUI()
    {
        rewardUI.SetActive(false);
        ClearRewardCardsParent();
        Time.timeScale = 1f;
    }

    private void AddCardToDeck(Ability ability)
    {
        deck.Add(ability);
        drawPile.Add(ability);
        UpdateDeckText();
        UpdatePileUIs(); // draw pile back & count changed
    }

    private Rarity RollRarity()
    {
        float roll = Random.value;

        if (roll < 0.005f) return Rarity.Legendary;
        if (roll < 0.03f) return Rarity.Epic;
        if (roll < 0.10f) return Rarity.Rare;
        if (roll < 0.30f) return Rarity.Uncommon;
        return Rarity.Common;
    }
}
