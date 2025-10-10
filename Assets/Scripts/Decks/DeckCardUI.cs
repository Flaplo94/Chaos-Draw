using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DeckCardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    [Header("Data")]
    public DeckDefinition deck;

    [Header("Base UI")]
    [SerializeField] private Image cardImage;
    [SerializeField] private GameObject lockOverlay;
    [SerializeField] private Image selectedGlow;
    [SerializeField] private Image hoverGlow;
    [SerializeField] private RectTransform tiltRoot;

    [Header("Unlock UI on overlay")]
    [SerializeField] private Button unlockButton;        // ligger på overlayet
    [SerializeField] private TMP_Text unlockLabel;       // "Unlock for 500"
    [SerializeField] private GameObject notEnoughHint;   // lille tekst – vises KUN efter mislykket køb
    [SerializeField] private float notEnoughSeconds = 1.5f;

    [Header("Hover Feel")]
    [SerializeField] private float tiltAmount = 6f;
    [SerializeField] private float lerpSpeed = 10f;
    [SerializeField] private float hoverScale = 1.02f;

    public bool IsUnlocked { get; private set; }

    // events sættes fra ChooseDeckMenu
    public Action<DeckCardUI> onSelected;
    public Action<DeckCardUI> onRequestUnlock;

    private bool isHovered;
    private Vector2 hoverPos;
    private Coroutine hintRoutine;

    void Awake()
    {
        if (selectedGlow) selectedGlow.enabled = false;
        if (hoverGlow) hoverGlow.enabled = false;

        var btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => onSelected?.Invoke(this)); // åbner detaljer
            btn.interactable = true;
        }

        if (unlockButton != null)
        {
            unlockButton.onClick.RemoveAllListeners();
            unlockButton.onClick.AddListener(() => onRequestUnlock?.Invoke(this)); // forsøger køb
        }

        if (!tiltRoot) tiltRoot = transform as RectTransform;
        Bind();
    }

    public void Bind()
    {
        if (deck && cardImage)
        {
            cardImage.sprite = deck.icon;
            cardImage.color = deck.uiTint;
        }

        IsUnlocked = DeckUnlocks.IsUnlocked(deck);

        if (lockOverlay) lockOverlay.SetActive(!IsUnlocked);
        if (unlockButton) unlockButton.gameObject.SetActive(!IsUnlocked);
        HideNotEnoughImmediate();

        ResetVisual();
    }

    /// <summary>
    /// Opdaterer prislabel. Knappen forbliver klikbar selv hvis man ikke har råd,
    /// så vi kan vise "Not enough" først NÅR man forsøger at købe.
    /// </summary>
    public void ConfigureUnlockUI(int price, int currentShards)
    {
        if (IsUnlocked)
        {
            if (unlockButton) unlockButton.gameObject.SetActive(false);
            if (lockOverlay) lockOverlay.SetActive(false);
            HideNotEnoughImmediate();
            return;
        }

        if (lockOverlay) lockOverlay.SetActive(true);
        if (unlockLabel) unlockLabel.text = "Unlock for " + price;

        if (unlockButton)
        {
            unlockButton.gameObject.SetActive(true);
            unlockButton.interactable = true; // vigtig
        }
        // Viser IKKE notEnoughHint her – kun ved mislykket køb.
    }

    public void ShowNotEnough()
    {
        if (!notEnoughHint) return;
        if (hintRoutine != null) StopCoroutine(hintRoutine);
        hintRoutine = StartCoroutine(FlashNotEnough());
    }

    private IEnumerator FlashNotEnough()
    {
        notEnoughHint.SetActive(true);
        yield return new WaitForSecondsRealtime(Mathf.Max(0.2f, notEnoughSeconds));
        notEnoughHint.SetActive(false);
        hintRoutine = null;
    }

    private void HideNotEnoughImmediate()
    {
        if (hintRoutine != null) { StopCoroutine(hintRoutine); hintRoutine = null; }
        if (notEnoughHint) notEnoughHint.SetActive(false);
    }

    public void SetSelected(bool on)
    {
        if (selectedGlow) selectedGlow.enabled = on;
    }

    public void OnPointerEnter(PointerEventData _) { isHovered = true; if (hoverGlow && IsUnlocked) hoverGlow.enabled = true; }
    public void OnPointerExit(PointerEventData _) { isHovered = false; if (hoverGlow) hoverGlow.enabled = false; }
    public void OnPointerMove(PointerEventData e) { hoverPos = e.position; }

    void Update()
    {
        if (!tiltRoot) return;
        Quaternion targetRot = Quaternion.identity;
        Vector3 targetScl = Vector3.one;

        if (isHovered && IsUnlocked)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(tiltRoot, hoverPos, null, out var local);
            var r = tiltRoot.rect;
            var nx = Mathf.Clamp(local.x / (r.width * 0.5f), -1f, 1f);
            var ny = Mathf.Clamp(local.y / (r.height * 0.5f), -1f, 1f);
            targetRot = Quaternion.Euler(-ny * tiltAmount, nx * tiltAmount, 0f);
            targetScl = Vector3.one * hoverScale;
        }

        tiltRoot.localRotation = Quaternion.Slerp(tiltRoot.localRotation, targetRot, Time.unscaledDeltaTime * lerpSpeed);
        tiltRoot.localScale = Vector3.Lerp(tiltRoot.localScale, targetScl, Time.unscaledDeltaTime * lerpSpeed);
    }

    void OnDisable() => ResetVisual();

    private void ResetVisual()
    {
        if (tiltRoot)
        {
            tiltRoot.localRotation = Quaternion.identity;
            tiltRoot.localScale = Vector3.one;
        }
        if (hoverGlow) hoverGlow.enabled = false;
        HideNotEnoughImmediate();
    }
}
