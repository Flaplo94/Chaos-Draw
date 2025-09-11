using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DeckCardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    [Header("Data")]
    public DeckDefinition deck;

    [Header("UI Refs")]
    [SerializeField] private Image cardImage;        // CardBack
    [SerializeField] private GameObject lockOverlay; // mørkt overlay/padlock
    [SerializeField] private Image selectedGlow;     // glow når valgt (kan være null)
    [SerializeField] private Image hoverGlow;        // glow ved hover (kan være null)
    [SerializeField] private RectTransform tiltRoot; // det visuelle indhold, der må roteres

    [Header("Hover Feel")]
    [SerializeField] private float tiltAmount = 6f;  // grader
    [SerializeField] private float lerpSpeed = 10f;
    [SerializeField] private float hoverScale = 1.02f;

    public bool IsUnlocked { get; private set; }
    public Action<DeckCardUI> onSelected;

    private bool isHovered;
    private Vector2 hoverPos;

    void Awake()
    {
        if (selectedGlow) selectedGlow.enabled = false;
        if (hoverGlow) hoverGlow.enabled = false;
        Bind();

        var btn = GetComponent<Button>();
        if (btn) btn.onClick.AddListener(() => { if (IsUnlocked) onSelected?.Invoke(this); });
        if (!tiltRoot) tiltRoot = transform as RectTransform;
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

        var btn = GetComponent<Button>();
        if (btn) btn.interactable = IsUnlocked;

        ResetVisual();
    }

    void Update()
    {
        if (!tiltRoot) return;

        // målrotation/skalering
        Quaternion targetRot = Quaternion.identity;
        Vector3 targetScl = Vector3.one;

        if (isHovered && IsUnlocked)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                tiltRoot, hoverPos, null, out var local);

            var rect = tiltRoot.rect;
            var norm = new Vector2(
                Mathf.Clamp(local.x / (rect.width * 0.5f), -1f, 1f),
                Mathf.Clamp(local.y / (rect.height * 0.5f), -1f, 1f)
            );

            targetRot = Quaternion.Euler(-norm.y * tiltAmount, norm.x * tiltAmount, 0f);
            targetScl = Vector3.one * hoverScale;
        }

        tiltRoot.localRotation = Quaternion.Slerp(tiltRoot.localRotation, targetRot, Time.unscaledDeltaTime * lerpSpeed);
        tiltRoot.localScale = Vector3.Lerp(tiltRoot.localScale, targetScl, Time.unscaledDeltaTime * lerpSpeed);
    }

    public void SetSelected(bool on)
    {
        if (selectedGlow) selectedGlow.enabled = on;
    }

    public void OnPointerEnter(PointerEventData _) { isHovered = true; if (hoverGlow && IsUnlocked) hoverGlow.enabled = true; }
    public void OnPointerExit(PointerEventData _) { isHovered = false; if (hoverGlow) hoverGlow.enabled = false; }
    public void OnPointerMove(PointerEventData e) { hoverPos = e.position; }

    void OnDisable() => ResetVisual();

    private void ResetVisual()
    {
        if (tiltRoot)
        {
            tiltRoot.localRotation = Quaternion.identity;
            tiltRoot.localScale = Vector3.one;
        }
        if (hoverGlow) hoverGlow.enabled = false;
    }
}
