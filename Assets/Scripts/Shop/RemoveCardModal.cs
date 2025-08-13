using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RemoveCardModalUI : MonoBehaviour
{
    public static RemoveCardModalUI Instance;

    [Header("References")]
    public GameObject root;          // Panelet (deaktiveret fra start)
    public Transform contentParent;  // "List" containeren hvor knapper spawnes
    public GameObject buttonPrefab;  // "ListButton" prefab (Button – TextMeshPro)

    void Awake()
    {
        Instance = this;
        if (root != null) root.SetActive(false);
    }

    public static void Show()
    {
        if (Instance == null)
        {
            Debug.LogWarning("[RemoveCardModalUI] Instance mangler i scenen.");
            return;
        }
        Instance.BuildAndShow();
    }

    private void BuildAndShow()
    {
        // Ryd gamle knapper
        if (contentParent != null)
        {
            for (int i = contentParent.childCount - 1; i >= 0; i--)
                Destroy(contentParent.GetChild(i).gameObject);
        }

        var cards = (DeckManager.Instance != null) ? DeckManager.Instance.GetRemovableCards() : null;

        if (cards == null || cards.Count == 0)
        {
            var go = Instantiate(buttonPrefab, contentParent);
            var btn = go.GetComponent<Button>();
            if (btn) btn.interactable = false;

            var label = go.GetComponentInChildren<TMP_Text>();
            if (label) label.text = "(No removable cards)";
        }
        else
        {
            foreach (var so in cards.Distinct())
            {
                var go = Instantiate(buttonPrefab, contentParent);

                // Label = asset-navnet
                var label = go.GetComponentInChildren<TMP_Text>();
                if (label) label.text = string.IsNullOrEmpty(so.name) ? "Item" : so.name;

                var soRef = so; // capture for lambda

                var btn = go.GetComponent<Button>();
                if (btn)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() =>
                    {
                        if (DeckManager.Instance != null)
                            DeckManager.Instance.RemoveCard(soRef);
                        Close();
                    });
                }
            }
        }

        if (root != null) root.SetActive(true);
    }

    public void Close()
    {
        if (root != null) root.SetActive(false);
    }
}
