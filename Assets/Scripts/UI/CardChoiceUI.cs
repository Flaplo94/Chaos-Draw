using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardChoiceUI : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject choicePanel;
    public Button[] cardButtons;
    public Image[] cardIcons;
    public TextMeshProUGUI[] cardNames;

    [Header("Available Cards")]
    public List<CardData> availableCards;

    private void Start()
    {
        choicePanel.SetActive(false);
    }

    public void ShowCardChoices()
    {
        choicePanel.SetActive(true);

        // Vælg 3 tilfældige unikke kort
        List<CardData> choices = new List<CardData>();
        while (choices.Count < 3)
        {
            CardData randomCard = availableCards[Random.Range(0, availableCards.Count)];
            if (!choices.Contains(randomCard))
                choices.Add(randomCard);
        }

        // Fyld knapperne med data
        for (int i = 0; i < cardButtons.Length; i++)
        {
            int index = i;
            cardIcons[i].sprite = choices[i].icon;
            cardNames[i].text = choices[i].cardName;

            cardButtons[i].onClick.RemoveAllListeners();
            cardButtons[i].onClick.AddListener(() =>
            {
                AddCardToDeck(choices[index]);
                choicePanel.SetActive(false);
            });
        }
    }

    void AddCardToDeck(CardData card)
    {
        Debug.Log("Valgte kort: " + card.cardName);
        // TODO: Tilføj kort til deck her
    }
}
