using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class CardHandUI : MonoBehaviour
{
    [Header("Card UI")]
    [SerializeField] private CardSlotUI[] cardSlots;
    [SerializeField] private TextMeshProUGUI discardCounterText;

    // NEW: Deck counter text (how many left to draw)
    [SerializeField] private TextMeshProUGUI deckCounterText;

    [Header("Pile UI (face-down stacks)")]
    [SerializeField] private DeckSlotUI drawPileUI;
    [SerializeField] private DeckSlotUI discardPileUI;

    [Header("Card Backs (per magic type)")]
    [SerializeField] private Sprite fireBack;
    [SerializeField] private Sprite lightningBack;
    [SerializeField] private Sprite otherBack;
    [SerializeField] private Sprite emptyBack; // optional fallback when a pile is empty

    [Header("Card Pool")]
    [SerializeField] private List<StartingCard> startingDeckList = new();
    private List<Ability> allAbilities = new();

    [Header("Reward UI")]
    [SerializeField] private GameObject rewardUI;
    [SerializeField] private GameObject[] rewardCards;
    [SerializeField] private Button skipButton;

    private int waveCount = 0;
    private readonly List<Ability> deck = new();
    private readonly List<Ability> drawPile = new();
    private readonly List<Ability> discardPile = new();
    private readonly Ability[] hand = new Ability[4];

    // (Optional) You can remove discardCount and just display discardPile.Count everywhere.
    // Keeping it here only to minimize changes to your existing UI.
    // private int discardCount;

    [System.Serializable]
    public class StartingCard
    {
        public string abilityName;
        public int count;
    }

    private void Start()
    {
        allAbilities = new List<Ability>(Resources.LoadAll<Ability>(""));
        CreateStartingDeck();
        Shuffle(drawPile);

        UpdateDiscardText();
        UpdateDeckText();
        UpdatePileUIs();     // NEW: show backs + counts now

        DrawInitialHand();
    }

    private void Update()
    {
        if (Keyboard.current.digit1Key.wasPressedThisFrame) TryUseCard(0);
        if (Keyboard.current.digit2Key.wasPressedThisFrame) TryUseCard(1);
        if (Keyboard.current.digit3Key.wasPressedThisFrame) TryUseCard(2);
        if (Keyboard.current.digit4Key.wasPressedThisFrame) TryUseCard(3);
    }

    private void CreateStartingDeck()
    {
        deck.Clear();
        drawPile.Clear();
        discardPile.Clear();

        // discardCount = 0;
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
        UpdatePileUIs(); // NEW
    }

    private void DrawInitialHand()
    {
        for (int i = 0; i < hand.Length; i++)
            DrawCard(i);
    }

    private void DrawCard(int slotIndex)
    {
        // If draw pile is empty but all cards are in discard, reshuffle discard -> draw
        if (drawPile.Count == 0 && discardPile.Count == deck.Count)
        {
            drawPile.AddRange(discardPile);
            discardPile.Clear();
            Shuffle(drawPile);

            // discardCount = 0;
            UpdateDiscardText();
            UpdateDeckText();
            UpdatePileUIs(); // NEW

            // Fill empty hand slots now that we have a fresh draw pile
            for (int i = 0; i < hand.Length; i++)
                if (hand[i] == null)
                    DrawCard(i);
            return;
        }

        if (drawPile.Count == 0)
        {
            UpdateDeckText();
            UpdatePileUIs(); // NEW (will show emptyBack if provided)
            return;
        }

        Ability card = drawPile[0];
        drawPile.RemoveAt(0);
        UpdateDeckText();
        UpdatePileUIs(); // NEW: top back may change after removing

        hand[slotIndex] = card;

        if (cardSlots != null && slotIndex < cardSlots.Length && cardSlots[slotIndex] != null)
            cardSlots[slotIndex].Show(card, GetRarityColor(card.rarity));
    }

    private void TryUseCard(int index)
    {
        if (hand[index] == null) return;

        bool success = hand[index].Activate();
        if (!success) return;

        discardPile.Add(hand[index]);
        hand[index] = null;

        if (cardSlots != null && index < cardSlots.Length && cardSlots[index] != null)
            cardSlots[index].Clear();

        // discardCount++;
        UpdateDiscardText();
        UpdatePileUIs(); // NEW: top of discard could change

        DrawCard(index); // Draw will also update pile UI
    }

    private void UpdateDiscardText()
    {
        if (discardCounterText != null)
            discardCounterText.text = discardPile.Count.ToString(); // simplified & always correct
        else
            Debug.LogWarning("Discard counter text is not assigned!");
    }

    private void UpdateDeckText()
    {
        if (deckCounterText != null)
            deckCounterText.text = drawPile.Count.ToString();
        // else optional: Debug.LogWarning("Deck counter text is not assigned!");
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

    private Rarity RollRarity()
    {
        float roll = Random.value;

        if (roll < 0.005f) return Rarity.Legendary;
        if (roll < 0.03f) return Rarity.Epic;
        if (roll < 0.10f) return Rarity.Rare;
        if (roll < 0.30f) return Rarity.Uncommon;
        return Rarity.Common;
    }

    private Color GetRarityColor(Rarity rarity)
    {
        return rarity switch
        {
            Rarity.Common => Color.white,
            Rarity.Uncommon => Color.green,
            Rarity.Rare => Color.blue,
            Rarity.Epic => new Color(0.6f, 0f, 0.8f),
            Rarity.Legendary => Color.yellow,
            _ => Color.gray
        };
    }

    private void ShowRewardUI()
    {
        Time.timeScale = 0f;
        rewardUI.SetActive(true);

        List<Ability> pool = new List<Ability>(allAbilities);
        Shuffle(pool);

        for (int i = 0; i < 3; i++)
        {
            Ability baseAbility = pool[i];
            Ability abilityCopy = Instantiate(baseAbility);
            abilityCopy.rarity = RollRarity();

            var nameText = rewardCards[i].transform.Find("AbilityName")?.GetComponent<TextMeshProUGUI>();
            var artImage = rewardCards[i].transform.Find("AbilityArt")?.GetComponent<Image>();
            var descText = rewardCards[i].transform.Find("AbilityDescription")?.GetComponent<TextMeshProUGUI>();
            var buttonImage = rewardCards[i].GetComponent<Image>();

            if (nameText != null) nameText.text = abilityCopy.abilityName + " [" + abilityCopy.rarity + "]";
            if (artImage != null) { artImage.sprite = abilityCopy.icon; artImage.color = Color.white; }
            if (descText != null) descText.text = abilityCopy.description;
            if (buttonImage != null) buttonImage.color = GetRarityColor(abilityCopy.rarity);

            Button cardButton = rewardCards[i].GetComponent<Button>();
            if (cardButton != null)
            {
                cardButton.onClick.RemoveAllListeners();
                cardButton.onClick.AddListener(() =>
                {
                    AddCardToDeck(abilityCopy);
                    rewardUI.SetActive(false);
                    Time.timeScale = 1f;
                });
            }
        }

        skipButton.onClick.RemoveAllListeners();
        skipButton.onClick.AddListener(() =>
        {
            rewardUI.SetActive(false);
            Time.timeScale = 1f;
        });
    }

    private void AddCardToDeck(Ability ability)
    {
        deck.Add(ability);
        drawPile.Add(ability);
        UpdateDeckText();
        UpdatePileUIs(); // NEW: draw pile back & count changed
    }

    // ---------- NEW: Back selection helpers ----------

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
            return emptyBack != null ? emptyBack : otherBack;

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
}
