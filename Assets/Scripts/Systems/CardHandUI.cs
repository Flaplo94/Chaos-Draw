using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using System.Collections;

public class CardHandUI : MonoBehaviour
{
    public Image[] cardSlots; // Drag in your 4 slot Image objects
    public Sprite[] abilityCardSprites; // All possible card images

    private Sprite[] currentCards = new Sprite[4];
    private int discardCount = 0;

    [Header("Discard")]
    public TextMeshProUGUI discardCounterText;
    public Image discardSlotIcon;
    void Start()
    {
        DrawFullHand();
    }

    void Update()
    {

        if (Input.GetKeyDown(KeyCode.Alpha1))
            TryUseCard(0);
        if (Input.GetKeyDown(KeyCode.Alpha2))
            TryUseCard(1);
        if (Input.GetKeyDown(KeyCode.Alpha3))
            TryUseCard(2);
        if (Input.GetKeyDown(KeyCode.Alpha4))
            TryUseCard(3);
    }

    void DrawFullHand()
    {
        for (int i = 0; i < cardSlots.Length; i++)
        {
            DrawCard(i);
        }
    }

    void DrawCard(int slotIndex)
    {
        Sprite card = GetRandomCard();
        currentCards[slotIndex] = card;
        cardSlots[slotIndex].sprite = card;
        cardSlots[slotIndex].color = Color.white; // make sure it's visible
    }

    Sprite GetRandomCard()
    {
        return abilityCardSprites[Random.Range(0, abilityCardSprites.Length)];
    }

    public void UseCard(int slotIndex)
    {
        Debug.Log("Used card in slot " + slotIndex);

        currentCards[slotIndex] = null;
        cardSlots[slotIndex].sprite = null;
        cardSlots[slotIndex].color = new Color(1, 1, 1, 0); // transparent

        // Discard logic
        discardCount++;
        UpdateDiscardText();
    }

    void TryUseCard(int slotIndex)
    {
        if (currentCards[slotIndex] == null)
        {
            Debug.Log("Slot " + (slotIndex + 1) + " is empty.");
            return;
        }

        UseCard(slotIndex);
    }

    void UpdateDiscardText()
    {
        discardCounterText.text = discardCount.ToString();
    }
}