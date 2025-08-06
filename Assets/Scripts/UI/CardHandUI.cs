using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class CardHandUI : MonoBehaviour
{
    [Header("Card UI")]
    [SerializeField] private Image[] cardSlots;
    [SerializeField] private TextMeshProUGUI discardCounterText;

    [Header("Card Pool")]
    [SerializeField] private List<StartingCard> startingDeckList = new();
    private List<Ability> allAbilities = new();

    [Header("Reward UI")]
    [SerializeField] private GameObject rewardUI;
    [SerializeField] private Button[] rewardButtons;
    [SerializeField] private Button skipButton;


    private int waveCount = 0;
    private List<Ability> deck = new();
    private List<Ability> drawPile = new();
    private List<Ability> discardPile = new();
    private Ability[] hand = new Ability[4];

    private int discardCount;

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

        foreach (var entry in startingDeckList)
        {
            Ability match = allAbilities.Find(a => a.name == entry.abilityName);
            if (match == null)
            {
                Debug.LogWarning("Ability not found: " + entry.abilityName);
                continue;
            }

            for (int i = 0; i < entry.count; i++)
            {
                deck.Add(match);
            }
        }

        drawPile.AddRange(deck);
    }

    private void DrawInitialHand()
    {
        for (int i = 0; i < hand.Length; i++)
            DrawCard(i);
    }

    private void DrawCard(int slotIndex)
    {
        // Reshuffle if needed
        if (drawPile.Count == 0 && discardPile.Count == deck.Count)
        {
            drawPile.AddRange(discardPile);
            discardPile.Clear();
            Shuffle(drawPile);
            discardCount = 0;
            UpdateDiscardText();

            // Refill all empty slots after reshuffle
            for (int i = 0; i < hand.Length; i++)
            {
                if (hand[i] == null)
                    DrawCard(i);
            }
            return;
        }

        if (drawPile.Count == 0) return;

        Ability card = drawPile[0];
        drawPile.RemoveAt(0);
        hand[slotIndex] = card;

        cardSlots[slotIndex].sprite = card.icon;
        cardSlots[slotIndex].color = Color.white;
    }

    private void TryUseCard(int index)
    {
        if (hand[index] == null) return;

        bool success = hand[index].Activate();

        if (!success) return;

        discardPile.Add(hand[index]);
        hand[index] = null;

        cardSlots[index].sprite = null;
        cardSlots[index].color = new Color(1, 1, 1, 0);

        discardCount++;
        UpdateDiscardText();

        DrawCard(index);
    }

    private void UpdateDiscardText()
    {
        if (discardCounterText != null)
            discardCounterText.text = discardCount.ToString();
        else
            Debug.LogWarning("Discard counter text is not assigned!");
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
        if (waveCount % 5 == 0)
        {
            ShowRewardUI();
        }
    }

    private void ShowRewardUI()
    {
        Time.timeScale = 0f;
        rewardUI.SetActive(true);

        List<Ability> pool = new List<Ability>(allAbilities);
        Shuffle(pool);

        for (int i = 0; i < 3; i++)
        {
            Ability ability = pool[i];

            var icon = rewardButtons[i].transform.Find("AbilityIcon").GetComponent<Image>();
            var nameText = rewardButtons[i].transform.Find("AbilityName").GetComponent<TextMeshProUGUI>();

            icon.sprite = ability.icon;
            icon.color = Color.white;

            nameText.text = ability.abilityName;

            rewardButtons[i].onClick.RemoveAllListeners();
            rewardButtons[i].onClick.AddListener(() =>
            {
                AddCardToDeck(ability);
                rewardUI.SetActive(false);
                Time.timeScale = 1f;
            });
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
    }

}
