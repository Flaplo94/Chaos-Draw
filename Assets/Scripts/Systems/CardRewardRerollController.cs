using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

public class CardRewardRerollController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button rerollButton;
    [SerializeField] private TMP_Text rerollLabel;

    [Header("Hooks")]
    [Tooltip("Kald din eksisterende funktion til at generere 3 nye, distinkte kort.")]
    public UnityEvent OnRequestReroll;

    [Header("Optional Notifier")]
    [Tooltip("Hvis du har en notifier med en public void ShowToast(string), reference den her.")]
    public MonoBehaviour notifier; // expected to have method: void ShowToast(string)
    private System.Reflection.MethodInfo showToastMethod;

    private void Awake()
    {
        if (rerollButton != null)
        {
            rerollButton.onClick.RemoveListener(OnClickReroll);
            rerollButton.onClick.AddListener(OnClickReroll);
        }

        if (notifier != null)
            showToastMethod = notifier.GetType().GetMethod("ShowToast", new[] { typeof(string) });
    }

    private void OnEnable()
    {
        RefreshUI();
    }

    public void RefreshUI()
    {
        var meta = MetaProgressionManager.Instance;
        int remaining = (meta != null) ? meta.GetRerollsRemaining() : 0;

        if (rerollLabel != null)
        {
            if (remaining > 1) rerollLabel.text = remaining + " rerolls available";
            else if (remaining == 1) rerollLabel.text = "1 reroll available";
            else rerollLabel.text = "No rerolls";
        }

        if (rerollButton != null)
            rerollButton.interactable = remaining > 0;

        // Grey/mørk style kan du styre via Button colors i Inspector;
        // interaktibilitet -> grå knap når 0.
    }

    private void OnClickReroll()
    {
        var meta = MetaProgressionManager.Instance;
        if (meta == null) return;

        if (!meta.TryConsumeReroll())
        {
            RefreshUI();
            return;
        }

        // Fortæl UI at vi har brugt et reroll
        RefreshUI();

        // Invoke din eksisterende generator (skal sikre 3 distinkte kort som normalt)
        if (OnRequestReroll != null)
            OnRequestReroll.Invoke();

        // Toast "Cards Rerolled"
        TryToast("Cards Rerolled");
    }

    private void TryToast(string msg)
    {
        if (showToastMethod == null || notifier == null) return;
        showToastMethod.Invoke(notifier, new object[] { msg });
    }
}
