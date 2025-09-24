using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ChooseDeckMenu : MonoBehaviour
{
    [Header("Parents")]
    [SerializeField] private Transform deckGrid;          // DeckGrid

    [Header("Detail Root")]
    [SerializeField] private GameObject detailPanelRoot;  //  drag DetailPanel her
    [SerializeField] private Image banner;
    [SerializeField] private TMP_Text deckName;
    [SerializeField] private TMP_Text tagline;            //  drag din "Taglines" TMP her
    [SerializeField] private Button startButton;
    [SerializeField] private string gameSceneName = "GameScene";



    private DeckCardUI[] cards;
    private int selectedIndex = -1;

    void OnEnable()
    {
        cards = deckGrid.GetComponentsInChildren<DeckCardUI>(true);
        foreach (var c in cards)
        {
            c.Bind();
            c.onSelected = OnCardSelected;
            c.SetSelected(false);
        }

        // Ingen default selection
        selectedIndex = -1;

        if (detailPanelRoot) detailPanelRoot.SetActive(false);
        if (banner) banner.sprite = null;
        if (deckName) deckName.text = "";
        if (tagline) tagline.text = "";

        if (startButton)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(StartSelected);
            startButton.interactable = false;
            startButton.gameObject.SetActive(false);
        }
    }

    private void OnCardSelected(DeckCardUI card)
    {
        int idx = System.Array.IndexOf(cards, card);
        if (idx >= 0) SelectIndex(idx);
    }

    private void SelectIndex(int idx)
    {
        if (cards == null || cards.Length == 0) return;

        if (selectedIndex >= 0 && selectedIndex < cards.Length)
            cards[selectedIndex].SetSelected(false);

        selectedIndex = Mathf.Clamp(idx, 0, cards.Length - 1);
        cards[selectedIndex].SetSelected(true);

        var d = cards[selectedIndex].deck;

        if (detailPanelRoot) detailPanelRoot.SetActive(true);
        if (banner) banner.sprite = d ? d.banner : null;
        if (deckName) deckName.text = d ? d.deckName : "";
        if (tagline) tagline.text = d ? d.tagline : "";

        if (startButton)
        {
            startButton.gameObject.SetActive(true);
            startButton.interactable = cards[selectedIndex].IsUnlocked;
        }
    }

    private void StartSelected()
    {
        if (selectedIndex < 0) return;
        var d = cards[selectedIndex].deck;
        if (!d) return;

        SessionData.SelectedDeck = d.id;
        // map deck-id -> element (tilføj bare flere cases når du får flere decks)
        SessionData.SelectedElement = (d.id == "lightning") ? MagicType.Lightning : MagicType.Fire;

        // ... og så loader du scenen som du allerede gør:
        SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);

    }

}
