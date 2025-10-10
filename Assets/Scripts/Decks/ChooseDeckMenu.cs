using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
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

    [Header("Primary Button (Start when unlocked)")]
    [SerializeField] private Button startButton;
    [SerializeField] private TMP_Text startButtonLabel;
    [SerializeField] private string gameSceneName = "GameScene";

    [Header("Starting Cards UI")]
    [SerializeField] private TMP_Text startingTitle;
    [SerializeField] private Transform startingGridParent;
    [SerializeField] private GameObject miniCardPrefab;

    [Header("Healing gate (optional)")]
    [SerializeField] private bool includeHealingWhenUnlocked = true;
    [SerializeField] private NodeData healingNode;
    [SerializeField] private ScriptableObject healingCard;

    [Header("Unlock Settings")]
    [SerializeField] private int lightningUnlockPrice = 500;

    [Header("Unlock Message (optional)")]
    [SerializeField] private GameObject unlockMessageRoot;
    [SerializeField] private TMP_Text unlockMessageText;
    [SerializeField] private float unlockMessageSeconds = 2f;

    private DeckCardUI[] cards;
    private int selectedIndex = -1;
    private readonly List<GameObject> spawnedMini = new List<GameObject>();
    private Coroutine msgRoutine;

    void OnEnable()
    {
        cards = deckGrid.GetComponentsInChildren<DeckCardUI>(true);
        foreach (var c in cards)
        {
            c.Bind();
            c.onSelected = OnCardSelected;
            c.onRequestUnlock = OnCardRequestUnlock;
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
            startButton.interactable = false;
            startButton.gameObject.SetActive(false);
        }

        if (!startButtonLabel && startButton)
            startButtonLabel = startButton.GetComponentInChildren<TMP_Text>(true);

        HideUnlockMessageImmediate();
        ClearStartingGrid();

        RefreshTilesAffordability();
    }

    // ---- Tile interactions ----

    private void OnCardSelected(DeckCardUI card)
    {
        int idx = System.Array.IndexOf(cards, card);
        if (idx >= 0) SelectIndex(idx);
    }

    private void OnCardRequestUnlock(DeckCardUI card)
    {
        if (card == null || card.deck == null) return;

        // Forsøg unlock – få success/fail tilbage
        bool unlockedNow = TryUnlockDeck(card.deck, card);

        // Kun ved succes refresher vi hele UI’et (ellers sletter vi hintet igen)
        if (unlockedNow)
        {
            RefreshTilesAffordability();

            // Hvis dette kort er valgt, skal detailpanelets Start-knap opdateres
            int idx = System.Array.IndexOf(cards, card);
            if (idx >= 0 && idx == selectedIndex)
                SetupPrimaryButtonForDeck(card.deck);
        }
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
        SetupPrimaryButtonForDeck(d);

        HideUnlockMessageImmediate();
    }

    // ---- Detail Panel primary button ----

    private void SetupPrimaryButtonForDeck(DeckDefinition d)
    {
        if (!startButton) return;

        bool unlocked = DeckUnlocks.IsUnlocked(d);
        if (!unlocked)
        {
            startButton.gameObject.SetActive(false); // unlock sker på selve kortet
            return;
        }

        startButton.gameObject.SetActive(true);
        startButton.onClick.RemoveAllListeners();
        if (startButtonLabel) startButtonLabel.text = "Start";
        startButton.interactable = true;
        startButton.onClick.AddListener(StartSelected);
    }

    // ---- Unlock mechanics ----

    private int GetUnlockPrice(DeckDefinition d)
    {
        if (d != null && d.id == "lightning") return Mathf.Max(0, lightningUnlockPrice);
        return 0;
    }

    private bool TryUnlockDeck(DeckDefinition d, DeckCardUI sourceTile = null)
    {
        if (d == null) return false;
        int price = GetUnlockPrice(d);

        // Find et sikkert mål til hint
        DeckCardUI target = sourceTile ?? FindTileForDeck(d);

        if (DeckUnlocks.IsUnlocked(d))
            return false;

        if (MetaProgressionManager.Instance == null)
        {
            if (target != null) target.ShowNotEnough();
            Debug.LogWarning("[DeckUnlock] MetaProgressionManager missing -> ShowNotEnough()");
            return false;
        }

        int shards = MetaProgressionManager.Instance.GetShards();
        if (shards < price)
        {
            if (target != null) target.ShowNotEnough();
            Debug.Log("[DeckUnlock] Not enough shards (" + shards + "/" + price + ").");
            return false;
        }

        if (!MetaProgressionManager.Instance.SpendShards(price))
        {
            if (target != null) target.ShowNotEnough();
            Debug.Log("[DeckUnlock] Spend failed -> ShowNotEnough()");
            return false;
        }

        // Succes
        DeckUnlocks.Unlock(d);
        ShowUnlockMessage(d);
        Debug.Log("[DeckUnlock] Unlocked deck '" + d.id + "' for " + price + " shards.");
        return true;
    }



    private void RefreshTilesAffordability()
    {
        int shards = MetaProgressionManager.Instance != null ? MetaProgressionManager.Instance.GetShards() : 0;

        if (cards == null) return;
        for (int i = 0; i < cards.Length; i++)
        {
            var c = cards[i];
            if (c == null || c.deck == null) continue;

            c.Bind(); // opdatér lock state
            int price = GetUnlockPrice(c.deck);
            c.ConfigureUnlockUI(price, shards); // viser kun pris + knap (ikke "not enough")
        }
    }
    private DeckCardUI FindTileForDeck(DeckDefinition d)
    {
        if (d == null || cards == null) return null;
        for (int i = 0; i < cards.Length; i++)
        {
            var c = cards[i];
            if (c != null && c.deck == d) return c;
        }
        return null;
    }

    // ---- Starting cards preview ----

    private void BuildStartingCardsPreview(DeckDefinition d)
    {
        ClearStartingGrid();
        if (!d || startingGridParent == null || miniCardPrefab == null) return;

        var list = (d.startingCardsPreview != null)
            ? new List<ScriptableObject>(d.startingCardsPreview)
            : new List<ScriptableObject>();

        if (includeHealingWhenUnlocked && healingNode != null && healingCard != null)
        {
            if (SkillTreeManager.Instance != null && SkillTreeManager.Instance.IsUnlocked(healingNode.id))
                list.Add(healingCard);
        }

        var counts = new Dictionary<ScriptableObject, int>();
        for (int i = 0; i < list.Count; i++)
        {
            var so = list[i];
            if (so == null) continue;
            if (!counts.ContainsKey(so)) counts[so] = 0;
            counts[so]++;
        }

        foreach (var kvp in counts)
        {
            var go = Object.Instantiate(miniCardPrefab, startingGridParent);
            spawnedMini.Add(go);

            var ui = go.GetComponent<CardMiniPreviewUI>();
            if (ui != null)
            {
                ui.SetObject(kvp.Key, d.icon);
                ui.SetQuantity(kvp.Value);
            }
        }

        bool any = counts.Count > 0;
        if (startingTitle != null) startingTitle.gameObject.SetActive(any);
        startingGridParent.gameObject.SetActive(any);
    }

    private void ClearStartingGrid()
    {
        for (int i = 0; i < spawnedMini.Count; i++)
            if (spawnedMini[i] != null) Destroy(spawnedMini[i]);
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

    // ---- Unlock toast ----

    private void ShowUnlockMessage(DeckDefinition d)
    {
        if (!unlockMessageRoot && !unlockMessageText) return;

        string msg = "Deck unlocked";
        if (d != null && !string.IsNullOrEmpty(d.deckName)) msg = d.deckName + " unlocked";

        if (unlockMessageText) unlockMessageText.text = msg;
        if (msgRoutine != null) StopCoroutine(msgRoutine);
        msgRoutine = StartCoroutine(FlashUnlockMessage());
    }

    private IEnumerator FlashUnlockMessage()
    {
        if (unlockMessageRoot) unlockMessageRoot.SetActive(true);
        yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, unlockMessageSeconds));
        if (unlockMessageRoot) unlockMessageRoot.SetActive(false);
        msgRoutine = null;
    }

    private void HideUnlockMessageImmediate()
    {
        if (msgRoutine != null) { StopCoroutine(msgRoutine); msgRoutine = null; }
        if (unlockMessageRoot) unlockMessageRoot.SetActive(false);
    }
}
