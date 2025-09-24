using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using System.Collections;
using System.Linq; //  NY: til reward-filter

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

    public Sprite emptySlotSprite; // sprite som vises i h�nden, n�r slot er tomt

    [Header("Card Pool")]
    [SerializeField] private List<StartingCard> startingDeckList = new();
    private List<Ability> allAbilities = new();

    // --- NYT: Start-spells pr. deck (kan s�ttes i Inspector) ---
    [Header("Start Spells pr. Deck")]
    [SerializeField] private string fireStartAbilityName = "Fireball";
    [SerializeField] private string lightningStartAbilityName = "LightningBall";
    [SerializeField][Min(1)] private int startCopies = 4;

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

    private bool rewardOpen = false;
    private bool shuffleActive = false;

    public System.Action OnHandChanged;

    [SerializeField] private RectTransform selectionHighlight; // assign an Image in inspector
    private int selectedIndex = -1;

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

        // --- NY: Overskriv start-listen ud fra valgt element (Fire/Lightning) ---
        OverrideStartingDeckFromSelectedElement();

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
        {
            var ability = (index >= 0 && hand != null && index < hand.Length) ? hand[index] : null;
            if (ability != null && PlayerMana.Instance != null && PlayerMana.Instance.GetMana() < ability.manaCost)
            {
                // Optional: on-screen popup
                var uiMsg = Object.FindFirstObjectByType<UIMessage>();
                if (uiMsg != null) uiMsg.ShowMessage("Not enough mana!");

                // OR: flash the slot, play a sound, etc.
                return; // don�t try to cast
            }

            TryUseCard(index);
        }
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
        NotifyHandChanged();
    }

    private IEnumerator ShuffleWithDelay(List<Ability> list)
    {
        shuffleActive = true;
        StartShuffleSfx();

        float duration = Random.Range(shuffleDurationRange.x, shuffleDurationRange.y);
        yield return new WaitForSeconds(duration);

        StopShuffleSfx();
        shuffleActive = false;

        Shuffle(list);

        UpdateDiscardText();
        UpdateDeckText();
        UpdatePileUIs();

        for (int i = 0; i < hand.Length; i++)
            if (hand[i] == null) DrawCard(i);

        NotifyHandChanged();
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
        NotifyHandChanged();
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
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
            audioSource.loop = false;
        }
        rewardOpen = true;

        PauseManager.RequestPause();
        rewardUI.SetActive(true);
        ClearRewardCardsParent();

        // --- NY: filtr�r pool efter valgt element f�r vi v�lger 3 ---
        var element = SessionData.SelectedElement; // MagicType.Fire / MagicType.Lightning
        List<Ability> pool = allAbilities
            .Where(a => a != null && a.magicType == element)
            .ToList();

        Shuffle(pool);

        for (int i = 0; i < 3 && i < pool.Count; i++)
        {
            Ability abilityCopy = Instantiate(pool[i]);
            abilityCopy.name = pool[i].name;
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
        rewardOpen = false;

        if (shuffleActive) StartShuffleSfx();

        rewardUI.SetActive(false);
        skipButton.gameObject.SetActive(false);
        ClearRewardCardsParent();
        PauseManager.ReleasePause();
    }

    private void AddCardToDeck(Ability ability)
    {
        if (ability == null) return;

        // Player now owns the card either way
        deck.Add(ability);

        // If there�s room in hand, place it directly into the first empty slot
        int empty = FindFirstEmptyHandSlot();
        if (empty != -1)
        {
            hand[empty] = ability;
            if (cardSlots != null && empty < cardSlots.Length && cardSlots[empty] != null)
                cardSlots[empty].Show(ability);
            
            NotifyHandChanged();

            // Piles didn�t change, but keep UI consistent
            UpdateDiscardText();
            UpdateDeckText();
            UpdatePileUIs();
            return;
        }

        // Otherwise: old behavior � add to draw pile
        drawPile.Add(ability);
        UpdateDeckText();
        UpdatePileUIs();
        NotifyHandChanged();
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

        shuffleActive = true;
        StartShuffleSfx();

        yield return new WaitForSeconds(currentManualShuffleTime);

        StopShuffleSfx();
        shuffleActive = false;

        drawPile.AddRange(allCards);
        Shuffle(drawPile);

        UpdateDiscardText();
        UpdateDeckText();
        UpdatePileUIs();

        for (int i = 0; i < hand.Length; i++)
            DrawCard(i);

        currentManualShuffleTime += 2f;
        NotifyHandChanged();
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

        NotifyHandChanged();
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
            abilityCopy.name = allAbilities[idx].name;
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
    private int FindFirstEmptyHandSlot()
    {
        if (hand == null) return -1;
        for (int i = 0; i < hand.Length; i++)
            if (hand[i] == null) return i;
        return -1;
    }
    private void StartShuffleSfx()
    {
        if (!rewardOpen && audioSource != null && shuffleClip != null && !audioSource.isPlaying)
        {
            audioSource.clip = shuffleClip;
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    private void StopShuffleSfx()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.loop = false;
        }
    }

    // Selection API used by CardSelectionController
    public void SetSelectedIndex(int index)
    {
        selectedIndex = index;
        UpdateSelectionHighlight();
    }

    public int HandLength
    {
        get { return hand != null ? hand.Length : 0; }
    }

    public Ability GetAbilityAt(int index)
    {
        if (hand == null || index < 0 || index >= hand.Length) return null;
        return hand[index];
    }

    public int FindIndexByAbilityId(string id)
    {
        if (hand == null || string.IsNullOrEmpty(id)) return -1;
        for (int i = 0; i < hand.Length; i++)
        {
            var a = hand[i];
            if (a == null) continue;
            if (GetAbilityId(a) == id) return i;
        }
        return -1;
    }

    private string GetAbilityId(Ability a)
    {
        var t = a.GetType();
        var f = t.GetField("internalID") ?? t.GetField("abilityId") ?? t.GetField("id");
        if (f != null) { var v = f.GetValue(a); if (v != null) return v.ToString(); }
        var p = t.GetProperty("InternalID") ?? t.GetProperty("AbilityId") ?? t.GetProperty("Id");
        if (p != null) { var v = p.GetValue(a, null); if (v != null) return v.ToString(); }
        return a.name;
    }

    private void UpdateSelectionHighlight()
    {
        if (selectionHighlight == null || cardSlots == null) return;

        // Hide when invalid
        if (selectedIndex < 0 || selectedIndex >= cardSlots.Length || cardSlots[selectedIndex] == null)
        {
            selectionHighlight.gameObject.SetActive(false);
            return;
        }

        var slotRect = cardSlots[selectedIndex].GetComponent<RectTransform>();
        selectionHighlight.gameObject.SetActive(true);

        // Move into the slot and stretch
        selectionHighlight.SetParent(slotRect, false);
        selectionHighlight.anchorMin = new Vector2(0, 0);
        selectionHighlight.anchorMax = new Vector2(1, 1);
        selectionHighlight.offsetMin = Vector2.zero;
        selectionHighlight.offsetMax = Vector2.zero;

        // Put BEHIND the card art
        selectionHighlight.SetSiblingIndex(0);

        // Never block clicks
        var img = selectionHighlight.GetComponent<UnityEngine.UI.Image>();
        if (img) img.raycastTarget = false;
    }

    private void NotifyHandChanged()
    {
        if (OnHandChanged != null) OnHandChanged.Invoke();
    }
    public void UseCardFromSelection(int index)
    {
        TryUseCardIfPossible(index); // <- was calling TryUseCard(...) before
    }
    // ------------ NY Hj�lper: v�lg start-deck ud fra valgt element ------------
    private void OverrideStartingDeckFromSelectedElement()
    {
        var element = SessionData.SelectedElement; // sat i ChooseDeckMenu ved Start

        string startName = (element == MagicType.Lightning)
            ? lightningStartAbilityName
            : fireStartAbilityName;

        startingDeckList.Clear();
        startingDeckList.Add(new StartingCard { abilityName = startName, count = startCopies });
    }
}
