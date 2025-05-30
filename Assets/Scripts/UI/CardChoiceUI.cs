using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

public class CardChoiceUI : MonoBehaviour
{
    public CardChoiceUI cardChoiceUI;

    [Serializable]
    public class CardButtonUI
    {
        public Button button;
        public Image icon;
        public TMP_Text nameText;
        public TMP_Text descriptionText;
    }

    public CanvasGroup canvasGroup;
    public List<CardButtonUI> cardButtons;
    private Action<CardData> onCardSelected;

    public void ShowCards(List<CardData> cards, Action<CardData> onSelected)
    {
        canvasGroup.alpha = 1;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;

        onCardSelected = onSelected;

        for (int i = 0; i < cardButtons.Count; i++)
        {
            var ui = cardButtons[i];
            var data = cards[i];

            ui.icon.sprite = data.icon;
            ui.nameText.text = data.cardName;
            ui.descriptionText.text = data.description;

            ui.button.onClick.RemoveAllListeners();
            ui.button.onClick.AddListener(() => SelectCard(data));
        }
    }

    private void SelectCard(CardData chosenCard)
    {
        Hide();
        onCardSelected?.Invoke(chosenCard);
    }

    private void Hide()
    {
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }
}
