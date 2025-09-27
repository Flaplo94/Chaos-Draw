using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using ChaosDraw.SkillTree;

public class ChooseDeckMenu : MonoBehaviour
{
    [Header("Parents")]
    [SerializeField] private Transform deckGrid;

    [Header("Detail Root")]
    [SerializeField] private GameObject detailPanelRoot;
    [SerializeField] private Image banner;
    [SerializeField] private TMP_Text deckName;
    [SerializeField] private TMP_Text tagline;
    [SerializeField] private Button startButton;
    [SerializeField] private string gameSceneName = "GameScene";

    [Header("Starting Cards UI (i DetailPanel)")]
    [SerializeField] private TMP_Text startingTitle;
    [SerializeField] private Transform startingGridParent;
    [SerializeField] private GameObject miniCardPrefab;

    [Header("Healing gate (valgfri)")]
    [SerializeField] private bool includeHealingWhenUnlocked = true;
    [SerializeField] private NodeData healingNode;          // NodeData (id bruges)
    [SerializeField] private ScriptableObject healingCard;  // ScriptableObject for Healing

    private DeckCardUI[] cards;
    private int selectedIndex = -1;
    private readonly List<GameObject> spawnedMini = new List<GameObject>();

    void OnEnable()
    {
        cards = deckGrid.GetComponentsInChildren<DeckCardUI>(true);
        foreach (var c in cards)
        {
            c.Bind();
            c.onSelected = OnCardSelected;
            c.SetSelected(false);
        }

        selectedIndex = -1;

        if (detailPanelRoot) detailPanelRoot.SetActive(false);
        if (banner) banner.sprite = null;
        if (deckName) deckName.text = "";
        if (tagline) tagline.text = "";
        if (startingTitle) startingTitle.text = "Starting Cards";

        if (startButton)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(StartSelected);
            startButton.interactable = false;
            startButton.gameObject.SetActive(false);
        }

        ClearStartingGrid();
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

        BuildStartingCardsPreview(d);

        if (startButton)
        {
            startButton.gameObject.SetActive(true);
            startButton.interactable = cards[selectedIndex].IsUnlocked;
        }
    }

    private void BuildStartingCardsPreview(DeckDefinition d)
    {
        ClearStartingGrid();
        if (!d || startingGridParent == null || miniCardPrefab == null) return;

        // 1) Kopier preview-listen
        var list = (d.startingCardsPreview != null)
            ? new List<ScriptableObject>(d.startingCardsPreview)
            : new List<ScriptableObject>();

        // 2) Tilføj Healing hvis unlocked
        if (includeHealingWhenUnlocked && healingNode != null && healingCard != null)
        {
            if (SkillTreeManager.Instance != null && SkillTreeManager.Instance.IsUnlocked(healingNode.id))
            {
                list.Add(healingCard);
            }
        }

        // 3) Gruper efter SO og tæl antal (så vi kan vise xN)
        var counts = new Dictionary<ScriptableObject, int>();
        for (int i = 0; i < list.Count; i++)
        {
            var so = list[i];
            if (so == null) continue;
            if (!counts.ContainsKey(so)) counts[so] = 0;
            counts[so]++;
        }

        // 4) Instantiér én mini pr. unik SO og sæt quantity
        foreach (var kvp in counts)
        {
            var so = kvp.Key;
            var qty = kvp.Value;

            var go = Object.Instantiate(miniCardPrefab, startingGridParent);
            spawnedMini.Add(go);

            var ui = go.GetComponent<CardMiniPreviewUI>();
            if (ui != null)
            {
                ui.SetObject(so, d.icon);
                ui.SetQuantity(qty);
            }
        }

        bool any = counts.Count > 0;
        if (startingTitle != null) startingTitle.gameObject.SetActive(any);
        startingGridParent.gameObject.SetActive(any);
    }

    private void ClearStartingGrid()
    {
        for (int i = 0; i < spawnedMini.Count; i++)
        {
            if (spawnedMini[i] != null) Destroy(spawnedMini[i]);
        }
        spawnedMini.Clear();
    }

    private void StartSelected()
    {
        if (selectedIndex < 0) return;
        var d = cards[selectedIndex].deck;
        if (!d) return;

        SessionData.SelectedDeck = d.id;
        SessionData.SelectedElement = (d.id == "lightning") ? MagicType.Lightning : MagicType.Fire;

        SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
    }
}
