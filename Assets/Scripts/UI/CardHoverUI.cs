using UnityEngine;
using TMPro;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas))]
public class CardHoverUI : MonoBehaviour
{
    public static CardHoverUI Instance;

    [Header("References")]
    [SerializeField] private Transform previewParent;
    [SerializeField] private GameObject rewardCardPrefab; // drag jeres reward card prefab ind her

    [Header("Sorting")]
    [SerializeField] private int sortingOrder = 200;

    [Header("Hover Card Size")]
    [SerializeField] private Vector2 hoverCardSize = new Vector2(600, 900); // kan justeres i Inspector

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
        Debug.Log("[CardHoverUI] Show start. ability=" + (ability != null) +
          " prefab=" + (rewardCardPrefab != null) +
          " parent=" + (previewParent != null));

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

            // Skalering i stedet for sizeDelta
            rt.localScale = Vector3.one * 2f; // 2x størrelse (kan justeres)
        }

        // udfyld UI med ability-data
        var ui = currentPreview.GetComponent<CardRewardUI>();
        if (ui != null)
        {
            ui.nameText.text = ability.abilityName;
            ui.artImage.sprite = ability.icon;
            ui.artImage.color = ability.icon ? Color.white : Color.clear;
            ui.artImage.preserveAspect = true;

            ui.rarityText.text = ability.rarity.ToString();
            ui.dmgText.text = ability.damage > 0 ? ability.damage.ToString() : "—";
            ui.manaText.text = ability.manaCost.ToString("0");

            var desc = currentPreview.transform.Find("AbilityDescription")?.GetComponent<TMP_Text>();
            if (desc != null)
                desc.text = ability.description;
        }

        // Fjern evt. interaktivitet (knappen skal ikke virke i hover-preview)
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
}
