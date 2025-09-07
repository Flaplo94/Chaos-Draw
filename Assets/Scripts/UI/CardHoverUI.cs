using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas))]
public class CardHoverUI : MonoBehaviour
{
    public static CardHoverUI Instance;

    [Header("References")]
    [SerializeField] private Transform previewParent;
    [SerializeField] private GameObject rewardCardPrefab; // brug CardRewardRoot

    [Header("Sorting")]
    [SerializeField] private int sortingOrder = 200;

    [Header("Hover Card Size")]
    [SerializeField] private Vector2 hoverCardSize = new Vector2(600, 900);

    private GameObject currentPreview;

    private void Awake()
    {
        Instance = this;

        var canvas = GetComponent<Canvas>();
        if (!canvas) canvas = gameObject.AddComponent<Canvas>();

        canvas.overrideSorting = true;
        canvas.sortingOrder = sortingOrder;

        gameObject.SetActive(false);
    }

    public void Show(Ability ability, Vector3 worldPos)
    {
        if (!ability || !rewardCardPrefab || !previewParent) return;

        // ryd tidligere preview
        if (currentPreview != null)
            Destroy(currentPreview);

        // lav nyt preview
        currentPreview = Instantiate(rewardCardPrefab, previewParent);

        // reset rect transform
        var rt = currentPreview.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one * 2f; // forstørret preview
        }

        // udfyld UI med Setup
        var ui = currentPreview.GetComponent<CardRewardUI>();
        if (ui != null)
        {
            ui.Setup(ability, CardHandUIInstance()?.GetRarityIcon(ability.rarity));
        }

        // Fjern interaktivitet i hover-preview
        var btn = currentPreview.GetComponent<Button>();
        if (btn) Destroy(btn);

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (currentPreview != null)
        {
            Destroy(currentPreview);
            currentPreview = null;
        }

        gameObject.SetActive(false);
    }

    private CardHandUI CardHandUIInstance()
    {
        return FindFirstObjectByType<CardHandUI>();
    }
}
