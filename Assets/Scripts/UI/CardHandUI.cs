using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using System.Collections;
using System.Linq; // til reward-filter
using ChaosDraw.SkillTree; // for NodeData + SkillTreeManager

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

    public Sprite emptySlotSprite;

    [Header("Card Pool")]
    [SerializeField] private List<StartingCard> startingDeckList = new();
    private List<Ability> allAbilities = new();

    [Header("Start Spells pr. Deck")]
    [SerializeField] private string fireStartAbilityName = "Fireball";
    [SerializeField] private string lightningStartAbilityName = "LightningBall";
    [SerializeField][Min(1)] private int startCopies = 4;

    [Header("Start Spells (New) — exact cards per element")]
    [SerializeField] private List<StartCardDef> fireStartDeck = new();      // e.g. 3x Fireball, 2x FireBird, 2x Shield
    [SerializeField] private List<StartCardDef> lightningStartDeck = new(); // e.g. 3x ChainLightning, 2x LightningBall, 2x Shield

    [Tooltip("If ON, starting deck will skip cards that are still locked.")]
    [SerializeField] private bool respectUnlocksForStartingDeck = false;


    [Header("Healing Starter (SkillTree-gated)")]
    [Tooltip("Node der unlocker Healing-starterkortet (fx 'healing_unlocked').")]
    [SerializeField] private NodeData healingUnlockNode;
    [Tooltip("Ability-navn paa Healing (matcher baade asset name og abilityName).")]
    [SerializeField] private string healingAbilityName = "Healing";
    [SerializeField][Min(1)] private int healingCopies = 1;

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

    
    private Dimmer dimmer;
    internal Sprite cardFrameSprite;

    private bool rewardOpen = false;
    private bool shuffleActive = false;

    public System.Action OnHandChanged;

    [SerializeField] private RectTransform selectionHighlight;
    private int selectedIndex = -1;

    [HideInInspector] public bool decklessEnabled = false;
    [Header("Hand draw rules")]
    [SerializeField] private bool drawReplacementOnUse = false;

    [Header("Card Use Cooldown")]
    [SerializeField] private float cardUseCooldown = 0.3f;
    private bool cardUseOnCooldown = false;
    [SerializeField] private CardHandArc handArc;


    [System.Serializable]
    public class StartingCard
    {
        public string abilityName;
        public int count;
    }
    [System.Serializable]
    public class StartCardDef
    {
        public string abilityName;
        [Min(1)] public int copies = 1;
    }
    private void Start()
    {
        dimmer = FindFirstObjectByType<Dimmer>();
        

        if (!handParent) return;

        cardSlots = handParent.GetComponentsInChildren<CardSlotUI>(true);
        if (cardSlots == null || cardSlots.Length == 0) return;

        hand = new Ability[cardSlots.Length];

        // Load all abilities from Resources
        allAbilities = new List<Ability>(Resources.LoadAll<Ability>(""));

        if (PlayerBuffManager.Instance != null)
            PlayerBuffManager.Instance.ResetRunBuffsToPersisted();

        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.artifacts.Clear();
            PlayerInventory.Instance.items.Clear();
        }
        PlayerBuffManager.Instance?.ResetRunBuffsToPersisted(alsoClearSaves: true);

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

        if (card1Action != null) { card1Action.performed += _ => TryUseCardIfPossible(0); card1Action.Enable(); }
        if (card2Action != null) { card2Action.performed += _ => TryUseCardIfPossible(1); card2Action.Enable(); }
        if (card3Action != null) { card3Action.performed += _ => TryUseCardIfPossible(2); card3Action.Enable(); }
        if (card4Action != null) { card4Action.performed += _ => TryUseCardIfPossible(3); card4Action.Enable(); }
        if (reshuffleAction != null)
        {
            reshuffleAction.performed += _ =>
            {
                Debug.Log("Discard+Redraw pressed!");
                if (dimmer != null && !dimmer.dimmerOn)
                {
                    DiscardHandAndRedrawAndRefill();
                    PlayerBuffManager.Instance?.ResetFlowCharges();
                }
            };
            reshuffleAction.Enable();
        }
        else
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
        if (cardUseOnCooldown) return;
        index = InputShuffleSystem.Map(index);

        if (dimmer != null && !dimmer.dimmerOn)
        {
            var ability = (index >= 0 && hand != null && index < hand.Length) ? hand[index] : null;
            if (ability != null && PlayerMana.Instance != null)
            {
                int effectiveCost = GetEffectiveManaCost(ability);
                if (PlayerMana.Instance.GetMana() < effectiveCost)
                {
                    var uiMsg = Object.FindFirstObjectByType<UIMessage>();
                    if (uiMsg != null) uiMsg.ShowMessage("Not enough mana!");
                    return;
                }
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

        // NEW: pick the element-specific list if present
        var element = SessionData.SelectedElement;
        List<StartCardDef> elementList = null;
        switch (element)
        {
            case MagicType.Fire:
                elementList = fireStartDeck;
                break;
            case MagicType.Lightning:
                elementList = lightningStartDeck;
                break;
            // add more cases here if you have more elements
            default:
                elementList = null;
                break;
        }

        bool usedElementList = elementList != null && elementList.Count > 0;

        if (usedElementList)
        {
            // Build from per-element exact list
            for (int i = 0; i < elementList.Count; i++)
            {
                var entry = elementList[i];
                if (string.IsNullOrWhiteSpace(entry.abilityName)) continue;

                var match = ResolveAbilityByName(entry.abilityName);
                if (match == null) continue;

                if (!respectUnlocksForStartingDeck || CardUnlocks.IsAbilityUnlocked(match))
                {
                    for (int c = 0; c < Mathf.Max(1, entry.copies); c++)
                        deck.Add(match);
                }
            }
        }
        else
        {
            // FALLBACK: your existing startingDeckList behavior (unchanged)
            foreach (var entry in startingDeckList)
            {
                Ability match = allAbilities.Find(a => a.name == entry.abilityName);
                if (match == null) continue;

                for (int i = 0; i < entry.count; i++)
                    deck.Add(match);
            }
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
        if (cardUseOnCooldown) return;
        if (hand == null || index < 0 || index >= hand.Length) return;
        if (hand[index] == null) return;

        bool success = hand[index].Activate();
        if (!success) return;

        PlayerBuffManager.Instance?.ConsumeOneFlowChargeIfActive();

        discardPile.Add(hand[index]);
        hand[index] = null;
        StartCoroutine(CardUseCooldownRoutine());

        cardSlots[index].Clear();

        UpdateDiscardText();
        UpdatePileUIs();
        if (drawReplacementOnUse)
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
                int effectiveCost = GetEffectiveManaCost(ability);
                bool notEnough = effectiveCost > currentMana;
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
        if (top == null)
            return emptyBack ? emptyBack : otherBack;
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
        // NEW: strip stray nulls once (cheap)
        for (int i = discardPile.Count - 1; i >= 0; i--)
            if (discardPile[i] == null) discardPile.RemoveAt(i);

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

        var element = SessionData.SelectedElement;
        List<Ability> pool = allAbilities
            .Where(a => a != null && (a.magicType == element || a.magicType == MagicType.Utility) && CardUnlocks.IsAbilityUnlocked(a))
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

    public void RegenerateChoices()
    {
        if (rewardUI == null || !rewardUI.activeSelf) return;

        ClearRewardCardsParent();

        var element = SessionData.SelectedElement;
        List<Ability> pool = allAbilities
            .Where(a => a != null && (a.magicType == element || a.magicType == MagicType.Utility) && CardUnlocks.IsAbilityUnlocked(a))
            .ToList();

        Shuffle(pool);

        for (int i = 0; i < 3 && i < pool.Count; i++)
        {
            Ability abilityCopy = Instantiate(pool[i]);
            abilityCopy.name = pool[i].name;
            abilityCopy.rarity = RollRarity();
            BuildRewardCard(abilityCopy);
        }
    }

    private void CloseRewardUI()
    {
        rewardOpen = false;

        if (shuffleActive) StartShuffleSfx();

        rewardUI.SetActive(false);
        if (skipButton != null) skipButton.gameObject.SetActive(false);
        ClearRewardCardsParent();

        // NEW: Fresh hand + full mana instantly on reward close (no cooldown)
        ResetHandAndManaImmediate();

        PauseManager.ReleasePause();
    }

    private void AddCardToDeck(Ability ability)
    {
        if (ability == null) return;

        deck.Add(ability);

        int empty = FindFirstEmptyHandSlot();
        if (empty != -1)
        {
            hand[empty] = ability;
            if (cardSlots != null && empty < cardSlots.Length && cardSlots[empty] != null)
                cardSlots[empty].Show(ability);

            NotifyHandChanged();
            UpdateDiscardText();
            UpdateDeckText();
            UpdatePileUIs();
            return;
        }

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

    public void OnWaveCompleted()
    {
        waveCount++;

        if (decklessEnabled)
        {
            AddRandomCardsToDeck(2);
            return;
        }

        if (waveCount % 2 == 0)
            ShowRewardUI();
    }

    public void EnableDecklessRuntime()
    {
        decklessEnabled = true;

        deck.Clear();
        drawPile.Clear();
        discardPile.Clear();
        UpdateDiscardText();
        UpdateDeckText();
        UpdatePileUIs();

        ClearHandSlots();

        Debug.Log("[Deckless] CardHandUI cleared.");
    }

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

    public void AddRandomCardsToDeck(int count)
    {
        if (allAbilities == null || allAbilities.Count == 0) return;

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
            abilityCopy.rarity = RollRarity();

            deck.Add(abilityCopy);
            drawPile.Add(abilityCopy);
        }

        Shuffle(drawPile);
        UpdateDeckText();
        UpdatePileUIs();

        if (hand != null)
        {
            for (int i = 0; i < hand.Length; i++)
                if (hand[i] == null) DrawCard(i);
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

    public void SetSelectedIndex(int index)
    {
        selectedIndex = index;
        UpdateSelectionHighlight();           // existing highlight logic

        if (handArc != null)
            handArc.SetSelectedIndex(selectedIndex);   // <-- vigtigt: giv buen besked
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

        bool invalid =
            selectedIndex < 0 ||
            selectedIndex >= cardSlots.Length ||
            cardSlots[selectedIndex] == null ||
            GetAbilityAt(selectedIndex) == null;

        if (invalid)
        {
            selectionHighlight.gameObject.SetActive(false);
            return;
        }

        // 1) Flyt highlight ind i den valgte slot og stræk til hele kortet
        var slotRect = cardSlots[selectedIndex].GetComponent<RectTransform>();
        if (slotRect == null)
        {
            selectionHighlight.gameObject.SetActive(false);
            return;
        }

        selectionHighlight.gameObject.SetActive(true);
        selectionHighlight.SetParent(slotRect, false);
        selectionHighlight.anchorMin = new Vector2(0f, 0f);
        selectionHighlight.anchorMax = new Vector2(1f, 1f);
        selectionHighlight.offsetMin = Vector2.zero;
        selectionHighlight.offsetMax = Vector2.zero;

        // 2) Læg highlight lige under ManaOverlay (ellers nær toppen som fallback)
        int targetIndex = Mathf.Max(0, slotRect.childCount - 1); // fallback: næstøverst
        var manaOverlay = slotRect.Find("ManaOverlay") as RectTransform;
        if (manaOverlay != null)
        {
            targetIndex = manaOverlay.GetSiblingIndex(); // lander lige før overlay
        }
        selectionHighlight.SetSiblingIndex(targetIndex);

        // 3) Sørg for at highlight ikke fanger input
        var img = selectionHighlight.GetComponent<Image>();
        if (img != null) img.raycastTarget = false;
    }


    private void NotifyHandChanged()
    {
        if (OnHandChanged != null) OnHandChanged.Invoke();
    }

    public void UseCardFromSelection(int index)
    {
        TryUseCardIfPossible(index);
    }

    private void OverrideStartingDeckFromSelectedElement()
    {
        var element = SessionData.SelectedElement;

        string startName = (element == MagicType.Lightning)
            ? lightningStartAbilityName
            : fireStartAbilityName;

        startingDeckList.Clear();
        startingDeckList.Add(new StartingCard { abilityName = startName, count = startCopies });

        TryInjectHealingIntoStartingList();
    }

    private void TryInjectHealingIntoStartingList()
    {
        if (!IsHealingUnlocked()) return;

        var heal = ResolveAbilityByName(healingAbilityName);
        if (heal == null) return;

        startingDeckList.Add(new StartingCard { abilityName = heal.name, count = Mathf.Max(1, healingCopies) });
    }

    private bool IsHealingUnlocked()
    {
        if (healingUnlockNode == null) return false;

        var stm = SkillTreeManager.Instance;
        if (stm != null)
        {
            try
            {
                if (stm.IsUnlocked(healingUnlockNode.id)) return true;
                if (stm.GetLevel(healingUnlockNode.id) > 0) return true;
            }
            catch { }
        }

        string id = string.IsNullOrEmpty(healingUnlockNode.id) ? healingUnlockNode.name : healingUnlockNode.id;
        string payload = PlayerPrefs.GetString("nodeLevels", "");
        if (!string.IsNullOrEmpty(payload))
        {
            string token = id + "=";
            int idx = payload.IndexOf(token, System.StringComparison.Ordinal);
            if (idx >= 0)
            {
                int start = idx + token.Length;
                int end = payload.IndexOf("|", start, System.StringComparison.Ordinal);
                string sub = (end >= 0) ? payload.Substring(start, end - start) : payload.Substring(start);
                if (int.TryParse(sub, out int lv) && lv > 0) return true;
            }
        }

        if (PlayerPrefs.GetInt("starter_healing", 0) == 1) return true;

        return false;
    }

    private Ability ResolveAbilityByName(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        for (int i = 0; i < allAbilities.Count; i++)
        {
            var a = allAbilities[i];
            if (a != null && string.Equals(a.name, name, System.StringComparison.OrdinalIgnoreCase))
                return a;
        }
        return null;
    }
    private void AddStartingCardsForSelectedElement(List<Ability> deck)
    {
        var element = SessionData.SelectedElement;

        // Choose list for current element
        List<StartCardDef> list = null;
        switch (element)
        {
            case MagicType.Fire: list = fireStartDeck; break;
            case MagicType.Lightning: list = lightningStartDeck; break;
            default: list = null; break; // add more elements if you have them
        }

        bool usedNewLists = false;

        // Use NEW per-element list if present
        if (list != null && list.Count > 0)
        {
            usedNewLists = true;

            foreach (var def in list)
            {
                if (string.IsNullOrWhiteSpace(def.abilityName)) continue;

                var ability = ResolveAbilityByName(def.abilityName);
                if (ability == null)
                {
                    Debug.LogWarning($"[CardHandUI] Start card not found: '{def.abilityName}'.");
                    continue;
                }

                // Optional: respect unlocks
                if (respectUnlocksForStartingDeck && !CardUnlocks.IsAbilityUnlocked(ability))
                    continue;

                int copies = Mathf.Max(1, def.copies);
                for (int i = 0; i < copies; i++)
                    deck.Add(ability);
            }
        }

        // Fallback to your legacy single-name + startCopies if list was empty
        if (!usedNewLists)
        {
            string legacyName = null;
            switch (element)
            {
                case MagicType.Fire: legacyName = fireStartAbilityName; break;
                case MagicType.Lightning: legacyName = lightningStartAbilityName; break;
            }

            if (!string.IsNullOrEmpty(legacyName))
            {
                var ability = ResolveAbilityByName(legacyName);
                if (ability == null)
                {
                    Debug.LogWarning($"[CardHandUI] Legacy start card not found: '{legacyName}'.");
                }
                else
                {
                    for (int i = 0; i < Mathf.Max(1, startCopies); i++)
                        deck.Add(ability);
                }
            }
        }
    }

    private static string Normalize(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        s = s.ToUpperInvariant();
        System.Text.StringBuilder sb = new System.Text.StringBuilder(s.Length);
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (c == ' ' || c == '_' || c == '-') continue;
            sb.Append(c);
        }
        return sb.ToString();
    }

    private static int GetEffectiveManaCost(Ability ability)
    {
        if (ability == null) return 0;

        int baseCost = ability.manaCost;

        float mult = 1f;
        float flat = 0f;

        var pbm = PlayerBuffManager.Instance;
        if (pbm != null)
        {
            mult = pbm.GetManaCostReductionMult();

            try
            {
                var m = pbm.GetType().GetMethod("GetManaCostFlat");
                if (m != null && m.ReturnType == typeof(float))
                    flat = (float)m.Invoke(pbm, null);
            }
            catch { }
        }

        float reduced = (baseCost / Mathf.Max(0.01f, mult)) - flat;
        return Mathf.Max(0, Mathf.CeilToInt(reduced));
    }

    private void DiscardHandAndRedrawAndRefill()
    {
        if (dimmer != null && dimmer.dimmerOn) return;
        StartCoroutine(DiscardHandCooldown());
    }

    private IEnumerator DiscardHandCooldown()
    {
        if (hand != null && cardSlots != null)
        {
            for (int i = 0; i < hand.Length; i++)
            {
                if (hand[i] != null)
                {
                    discardPile.Add(hand[i]);
                    hand[i] = null;
                    if (cardSlots[i] != null) cardSlots[i].Clear();
                }
            }
        }

        UpdateDiscardText();
        UpdatePileUIs();
        UpdateCardOverlays();
        NotifyHandChanged();

        yield return new WaitForSecondsRealtime(2f);

        int need = hand != null ? hand.Length : 0;
        if (drawPile.Count < need && discardPile.Count > 0)
        {
            drawPile.AddRange(discardPile);
            discardPile.Clear();
            StartCoroutine(ShuffleWithDelay(drawPile));
        }
        else
        {
            for (int i = 0; i < need; i++)
                if (hand[i] == null) DrawCard(i);
        }

        if (PlayerMana.Instance != null)
            PlayerMana.Instance.RefillToFull();

        UpdateDeckText();
        UpdateDiscardText();
        UpdatePileUIs();
        UpdateCardOverlays();
        NotifyHandChanged();
    }

    private IEnumerator CardUseCooldownRoutine()
    {
        cardUseOnCooldown = true;
        yield return new WaitForSeconds(cardUseCooldown);
        cardUseOnCooldown = false;
    }

    // ===== NEW: Fresh hand + full mana (no cooldown), to be used after Shop/Reward =====
    public void ResetHandAndManaImmediate()
    {
        // 1) Move current hand to discard and clear slots
        if (hand != null && cardSlots != null)
        {
            for (int i = 0; i < hand.Length; i++)
            {
                if (hand[i] != null)
                {
                    discardPile.Add(hand[i]);
                    hand[i] = null;
                    if (cardSlots[i] != null) cardSlots[i].Clear();
                }
            }
        }

        UpdateDiscardText();
        UpdatePileUIs();
        UpdateCardOverlays();
        NotifyHandChanged();

        // 2) Ensure enough cards in draw, shuffle instantly if needed
        int need = hand != null ? hand.Length : 0;
        if (drawPile.Count < need && discardPile.Count > 0)
        {
            drawPile.AddRange(discardPile);
            discardPile.Clear();
            Shuffle(drawPile); // instant, no delay
        }

        // 3) Draw to full hand
        for (int i = 0; i < need; i++)
            if (hand[i] == null) DrawCard(i);

        // 4) Full mana refill
        if (PlayerMana.Instance != null)
            PlayerMana.Instance.RefillToFull();

        // 5) Final UI sync
        UpdateDeckText();
        UpdateDiscardText();
        UpdatePileUIs();
        UpdateCardOverlays();
        NotifyHandChanged();
    }
    public void DiscardHandOnly()
    {
        if (hand == null || cardSlots == null) return;

        for (int i = 0; i < hand.Length; i++)
        {
            if (hand[i] != null)
            {
                discardPile.Add(hand[i]);
                hand[i] = null;
                if (cardSlots[i] != null) cardSlots[i].Clear();
            }
        }

        UpdateDiscardText();
        UpdatePileUIs();
        UpdateCardOverlays();
        NotifyHandChanged();
    }
    public void DrawFullHand()
    {
        if (hand == null) return;

        // count how many cards we need
        int need = 0;
        for (int i = 0; i < hand.Length; i++)
            if (hand[i] == null) need++;

        if (need <= 0) return;

        // If we don't have enough in draw, fold in discard and shuffle instantly (no coroutine/delay)
        if (drawPile.Count < need && discardPile.Count > 0)
        {
            drawPile.AddRange(discardPile);
            discardPile.Clear();
            Shuffle(drawPile); // instant shuffle
            UpdateDiscardText();
            UpdateDeckText();
            UpdatePileUIs();
        }

        // Now draw into all empty slots
        for (int i = 0; i < hand.Length; i++)
        {
            if (hand[i] == null)
                DrawCard(i); // uses your existing single-slot draw (will gracefully handle if deck still runs out)
        }

        UpdateDeckText();
        UpdatePileUIs();
        UpdateCardOverlays();
        NotifyHandChanged();
    }
    public bool DiscardOneRandomCard()
    {
        if (hand == null) return false;

        var idxs = new List<int>();
        for (int i = 0; i < hand.Length; i++)
            if (hand[i] != null) idxs.Add(i);
        if (idxs.Count == 0) return false;

        int slot = Random.Range(0, idxs.Count);

        var card = hand[slot];
        if (card == null) return false; // slot got cleared meanwhile; bail safely

        // add to discard only if non-null
        discardPile.Add(card);

        hand[slot] = null;
        if (cardSlots != null && slot < cardSlots.Length && cardSlots[slot] != null)
            cardSlots[slot].Clear();

        UpdateDiscardText();
        UpdatePileUIs();
        UpdateCardOverlays();
        NotifyHandChanged();
        return true;
    }
    public void DrawExactly(int count)
    {
        if (hand == null || count <= 0) return;

        // how many empty slots do we actually have?
        int empty = 0;
        for (int i = 0; i < hand.Length; i++)
            if (hand[i] == null) empty++;

        int want = Mathf.Min(count, empty);
        if (want <= 0) return;

        //  Up-front reshuffle if draw pile can't cover what we want
        if (drawPile.Count < want && discardPile.Count > 0)
        {
            drawPile.AddRange(discardPile);
            discardPile.Clear();
            Shuffle(drawPile); // instant
            UpdateDiscardText();
            UpdateDeckText();
            UpdatePileUIs();
        }

        int drawn = 0;
        while (drawn < want)
        {
            // find next empty slot
            int slot = -1;
            for (int i = 0; i < hand.Length; i++)
            {
                if (hand[i] == null) { slot = i; break; }
            }
            if (slot == -1) break;

            // Safeguard reshuffle mid-loop if we just ran out
            if (drawPile.Count == 0 && discardPile.Count > 0)
            {
                drawPile.AddRange(discardPile);
                discardPile.Clear();
                Shuffle(drawPile);
                UpdateDiscardText();
                UpdateDeckText();
                UpdatePileUIs();
            }

            if (drawPile.Count == 0) break; // nothing to draw, bail

            // draw ONE into that slot using your existing path
            DrawCard(slot);
            drawn++;
        }

        // final UI sync
        UpdateDeckText();
        UpdatePileUIs();
        UpdateCardOverlays();
        NotifyHandChanged();
    }
}
