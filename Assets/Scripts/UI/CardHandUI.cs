using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using System.Collections;

public class CardHandUI : MonoBehaviour
{
    [Header("Hand (slots under parent)")]
    [SerializeField] private Transform handParent;
    [SerializeField] private CardSlotUI[] cardSlots;

    [Header("Counters")]
    [SerializeField] private TextMeshProUGUI discardCounterText;
    [SerializeField] private TextMeshProUGUI deckCounterText;

    [Header("Pile UI (face-down stacks)")]
    [SerializeField] private DeckSlotUI drawPileUI;
    [SerializeField] private DeckSlotUI discardPileUI;

    [Header("Card Backs (per magic type)")]
    public Sprite fireBack;
    public Sprite lightningBack;
    public Sprite otherBack;
    public Sprite emptyBack;

    public Sprite emptySlotSprite; // sprite som vises i hånden, når slot er tomt

    [Header("Card Pool")]
    [SerializeField] private List<StartingCard> startingDeckList = new();
    private List<Ability> allAbilities = new();

    [Header("Reward UI (parent + prefab)")]
    [SerializeField] private GameObject rewardUI;
    [SerializeField] private Transform rewardCardsParent;
    [SerializeField] private GameObject rewardCardPrefab;
    [SerializeField] private Button skipButton;

    [Header("Rarity Icons")]
    [SerializeField] private Sprite commonIcon;
    [SerializeField] private Sprite uncommonIcon;
    [SerializeField] private Sprite rareIcon;
    [SerializeField] private Sprite epicIcon;
    [SerializeField] private Sprite legendaryIcon;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip shuffleClip;
    [SerializeField] private Vector2 shuffleDurationRange = new Vector2(1f, 2f);

    [Header("Input System")]
    [SerializeField] private InputActionAsset inputActions;

    private InputAction card1Action;
    private InputAction card2Action;
    private InputAction card3Action;
    private InputAction card4Action;
    private InputAction reshuffleAction;

    private int waveCount = 0;
    private readonly List<Ability> deck = new();
    private readonly List<Ability> drawPile = new();
    private readonly List<Ability> discardPile = new();
    private Ability[] hand;

    [SerializeField] private float manualShuffleBaseTime = 4f;
    private float currentManualShuffleTime;

    private Dimmer dimmer;
    internal Sprite cardFrameSprite;

    // Deckless toggle
    [HideInInspector] public bool decklessEnabled = false;

    [System.Serializable]
    public class StartingCard
    {
        public string abilityName;
        public int count;
    }

    private void Start()
    {
        dimmer = FindFirstObjectByType<Dimmer>();
        currentManualShuffleTime = manualShuffleBaseTime;

        if (!handParent) return;

        cardSlots = handParent.GetComponentsInChildren<CardSlotUI>(true);
        if (cardSlots == null || cardSlots.Length == 0) return;

        hand = new Ability[cardSlots.Length];

        allAbilities = new List<Ability>(Resources.LoadAll<Ability>(""));
        CreateStartingDeck();
        Shuffle(drawPile);

        UpdateDiscardText();
        UpdateDeckText();
        UpdatePileUIs();

        DrawInitialHand();
    }

    private void OnEnable()
    {
        if (inputActions == null) return;
        var playerMap = inputActions.FindActionMap("Player");

        card1Action = playerMap.FindAction("Card 1");
        card2Action = playerMap.FindAction("Card 2");
        card3Action = playerMap.FindAction("Card 3");
        card4Action = playerMap.FindAction("Card 4");
        reshuffleAction = playerMap.FindAction("Reshuffle");

        if (card1Action != null)
        {
            card1Action.performed += _ => TryUseCardIfPossible(0);
            card1Action.Enable();
        }
        if (card2Action != null)
        {
            card2Action.performed += _ => TryUseCardIfPossible(1);
            card2Action.Enable();
        }
        if (card3Action != null)
        {
            card3Action.performed += _ => TryUseCardIfPossible(2);
            card3Action.Enable();
        }
        if (card4Action != null)
        {
            card4Action.performed += _ => TryUseCardIfPossible(3);
            card4Action.Enable();
        }
        if (reshuffleAction != null)
        {
            reshuffleAction.performed += _ =>
            {
                Debug.Log("Reshuffle pressed!");
                if (dimmer != null && !dimmer.dimmerOn)
                    StartCoroutine(ManualShuffle());
            };
            reshuffleAction.Enable();
        }
        else if (reshuffleAction == null)
        {
            Debug.LogWarning("Reshuffle action not found in InputActionAsset2.");
        }

    }

    private void OnDisable()
    {
        if (card1Action != null) card1Action.Disable();
        if (card2Action != null) card2Action.Disable();
        if (card3Action != null) card3Action.Disable();
        if (card4Action != null) card4Action.Disable();
        if (reshuffleAction != null) reshuffleAction.Disable();
    }

    private void Update()
    {
        UpdateCardOverlays();
    }

    private void TryUseCardIfPossible(int index)
    {
        index = InputShuffleSystem.Map(index);
        if (dimmer != null && !dimmer.dimmerOn)
            TryUseCard(index);
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
            if (match == null) continue;

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

        if (drawPile.Count == 0 && discardPile.Count == deck.Count)
        {
            drawPile.AddRange(discardPile);
            discardPile.Clear();
            StartCoroutine(ShuffleWithDelay(drawPile));
            return;
        }

        if (drawPile.Count == 0)
        {
            hand[slotIndex] = null;
            cardSlots[slotIndex].Clear();
            UpdateDeckText();
            UpdatePileUIs();
            return;
        }

        Ability card = drawPile[0];
        drawPile.RemoveAt(0);
        UpdateDeckText();
        UpdatePileUIs();

        hand[slotIndex] = card;
        cardSlots[slotIndex].Show(card);
    }

    private IEnumerator ShuffleWithDelay(List<Ability> list)
    {
        float duration = Random.Range(shuffleDurationRange.x, shuffleDurationRange.y);

        if (audioSource != null && shuffleClip != null)
        {
            audioSource.clip = shuffleClip;
            audioSource.loop = true;
            audioSource.Play();
        }

        yield return new WaitForSeconds(duration);

        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.loop = false;
        }

        Shuffle(list);

        UpdateDiscardText();
        UpdateDeckText();
        UpdatePileUIs();

        for (int i = 0; i < hand.Length; i++)
            if (hand[i] == null) DrawCard(i);
    }

    private void TryUseCard(int index)
    {
        if (hand == null || index < 0 || index >= hand.Length) return;
        if (hand[index] == null) return;

        bool success = hand[index].Activate();
        if (!success) return;

        discardPile.Add(hand[index]);
        hand[index] = null;

        cardSlots[index].Clear();

        UpdateDiscardText();
        UpdatePileUIs();
        DrawCard(index);
    }

    // -------------------- UI helpers --------------------
    private void UpdateDiscardText()
    {
        if (discardCounterText) discardCounterText.text = discardPile.Count.ToString();
    }

    private void UpdateDeckText()
    {
        if (deckCounterText) deckCounterText.text = drawPile.Count.ToString();
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int rand = Random.Range(i, list.Count);
            (list[i], list[rand]) = (list[rand], list[i]);
        }
    }

    private void UpdateCardOverlays()
    {
        if (PlayerMana.Instance == null) return;
        float currentMana = PlayerMana.Instance.GetMana();

        for (int i = 0; i < cardSlots.Length; i++)
        {
            var ability = cardSlots[i].GetAbility();
            if (ability != null)
            {
                bool notEnough = ability.manaCost > currentMana;
                cardSlots[i].SetGreyedOut(notEnough);
            }
            else
            {
                cardSlots[i].SetGreyedOut(false);
            }
        }
    }

    private Sprite GetTopBackFromPile(List<Ability> pile)
    {
        if (pile == null || pile.Count == 0)
            return emptyBack ? emptyBack : otherBack;

        var top = ReferenceEquals(pile, discardPile) ? pile[^1] : pile[0];
        return GetBackFor(top.magicType);
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

    private void UpdatePileUIs()
    {
        if (drawPileUI != null)
            drawPileUI.Set(GetTopBackFromPile(drawPile), drawPile.Count);

        if (discardPileUI != null)
            discardPileUI.Set(GetTopBackFromPile(discardPile), discardPile.Count);
    }

    // -------------------- Rewards --------------------
    private void ClearRewardCardsParent()
    {
        if (!rewardCardsParent) return;
        for (int i = rewardCardsParent.childCount - 1; i >= 0; i--)
            Destroy(rewardCardsParent.GetChild(i).gameObject);
    }

    private void BuildRewardCard(Ability ability)
    {
        var cardGO = Instantiate(rewardCardPrefab, rewardCardsParent);

        var ui = cardGO.GetComponent<CardRewardUI>();
        if (ui != null)
            ui.Setup(ability, GetRarityIcon(ability.rarity));

        var button = cardGO.GetComponent<Button>();
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

    public Sprite GetRarityIcon(Rarity rarity)
    {
        return rarity switch
        {
            Rarity.Common => commonIcon,
            Rarity.Uncommon => uncommonIcon,
            Rarity.Rare => rareIcon,
            Rarity.Epic => epicIcon,
            Rarity.Legendary => legendaryIcon,
            _ => null
        };
    }

    private void ShowRewardUI()
    {
        PauseManager.RequestPause();
        rewardUI.SetActive(true);
        ClearRewardCardsParent();

        List<Ability> pool = new(allAbilities);
        Shuffle(pool);

        for (int i = 0; i < 3 && i < pool.Count; i++)
        {
            Ability abilityCopy = Instantiate(pool[i]);
            abilityCopy.rarity = RollRarity();
            BuildRewardCard(abilityCopy);
        }

        if (skipButton != null)
        {
            skipButton.gameObject.SetActive(true);
            skipButton.onClick.RemoveAllListeners();
            skipButton.onClick.AddListener(CloseRewardUI);
        }
    }

    private void CloseRewardUI()
    {
        rewardUI.SetActive(false);
        skipButton.gameObject.SetActive(false);
        ClearRewardCardsParent();
        PauseManager.ReleasePause();
    }

    private void AddCardToDeck(Ability ability)
    {
        deck.Add(ability);
        drawPile.Add(ability);
        UpdateDeckText();
        UpdatePileUIs();
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

    private IEnumerator ManualShuffle()
    {
        List<Ability> allCards = new();
        allCards.AddRange(drawPile);
        allCards.AddRange(discardPile);

        for (int i = 0; i < hand.Length; i++)
        {
            if (hand[i] != null)
            {
                allCards.Add(hand[i]);
                hand[i] = null;
                cardSlots[i].Clear();
            }
        }

        drawPile.Clear();
        discardPile.Clear();

        UpdateDiscardText();
        UpdateDeckText();
        UpdatePileUIs();

        if (audioSource != null && shuffleClip != null)
        {
            audioSource.clip = shuffleClip;
            audioSource.loop = true;
            audioSource.Play();
        }

        yield return new WaitForSeconds(currentManualShuffleTime);

        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.loop = false;
        }

        drawPile.AddRange(allCards);
        Shuffle(drawPile);

        UpdateDiscardText();
        UpdateDeckText();
        UpdatePileUIs();

        for (int i = 0; i < hand.Length; i++)
            DrawCard(i);

        currentManualShuffleTime += 2f;
    }

    public void OnWaveCompleted()
    {
        waveCount++;

        if (decklessEnabled)
        {
            // Deckless: Every wave grant exactly 2 random cards, no reward UI
            AddRandomCardsToDeck(2);
            return;
        }

        // Original behavior: every 2 waves show reward UI :contentReference[oaicite:7]{index=7}
        if (waveCount % 2 == 0)
            ShowRewardUI();
    }
    // Call when Deckless is enabled (clears deck/draw/discard/hand and UI)
    public void EnableDecklessRuntime()
    {
        decklessEnabled = true;

        // Clear deck piles
        deck.Clear();
        drawPile.Clear();
        discardPile.Clear();
        UpdateDiscardText();
        UpdateDeckText();
        UpdatePileUIs();

        // Clear hand (same pattern you use in ManualShuffle) :contentReference[oaicite:0]{index=0}
        ClearHandSlots();

        Debug.Log("[Deckless] CardHandUI cleared.");
    }

    // Clear the visible hand & internal array
    private void ClearHandSlots()
    {
        if (hand == null || cardSlots == null) return;
        for (int i = 0; i < hand.Length; i++)
        {
            hand[i] = null;
            if (cardSlots[i] != null) cardSlots[i].Clear();
        }
    }

    // Add N random cards to the deck (instanced like your reward UI does) :contentReference[oaicite:1]{index=1}
    public void AddRandomCardsToDeck(int count)
    {
        if (allAbilities == null || allAbilities.Count == 0) return;

        // Lazy init hand array if needed (in case Deckless enabled before Start finished)
        if (hand == null && handParent != null)
        {
            cardSlots = handParent.GetComponentsInChildren<CardSlotUI>(true);
            hand = new Ability[cardSlots.Length];
        }

        for (int i = 0; i < count; i++)
        {
            int idx = Random.Range(0, allAbilities.Count);
            Ability abilityCopy = Instantiate(allAbilities[idx]);
            abilityCopy.rarity = RollRarity(); // reuse your rarity logic :contentReference[oaicite:2]{index=2}

            deck.Add(abilityCopy);
            drawPile.Add(abilityCopy);
        }

        // Shuffle draw pile then auto-fill empty hand slots
        Shuffle(drawPile);                       // reuse your shuffle method :contentReference[oaicite:3]{index=3}
        UpdateDeckText();                        // keeps counters correct :contentReference[oaicite:4]{index=4}
        UpdatePileUIs();                         // updates pile sprites/counts :contentReference[oaicite:5]{index=5}

        // Draw into empty hand slots so the new cards are playable
        if (hand != null)
        {
            for (int i = 0; i < hand.Length; i++)
                if (hand[i] == null) DrawCard(i); // reuse your existing draw flow :contentReference[oaicite:6]{index=6}
        }
    }

}
