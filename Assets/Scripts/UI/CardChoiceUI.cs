using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

public class CardChoiceUI : MonoBehaviour
{
    [System.Serializable]
    public class CardButtonSlot
    {
        public Button button;
        public Image icon;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI descriptionText;
    }

    public CanvasGroup canvasGroup;
    public List<CardButtonSlot> cardButtons = new List<CardButtonSlot>();
    private Action<CardData> onCardSelected;

    public void ShowCards(List<CardData> cards, Action<CardData> onSelected)
    {
        canvasGroup.alpha = 1;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        onCardSelected = onSelected;

        for (int i = 0; i < cardButtons.Count; i++)
        {
            var card = cards[i];
            var slot = cardButtons[i];

            slot.nameText.text = card.cardName;
            slot.descriptionText.text = card.description;
            slot.icon.sprite = card.cardSprite;

            int index = i;
            slot.button.onClick.RemoveAllListeners();
            slot.button.onClick.AddListener(() => SelectCard(cards[index]));
        }
    }

    public void Hide()
    {
        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void SelectCard(CardData selectedCard)
    {
        onCardSelected?.Invoke(selectedCard);
        Hide();
    }
}
