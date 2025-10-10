using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class CardSelectionController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private CardHandUI handUI;
    [SerializeField] private SmartcastIndicator indicator;

    [Header("Input System")]
    [SerializeField] private InputActionReference prevCardAction;   // Q
    [SerializeField] private InputActionReference nextCardAction;   // E
    [SerializeField] private InputActionReference smartcastAction;  // RMB

    [Header("Behavior")]
    [SerializeField] private bool stickySelection = true;

    
    public Action<Ability, Vector2> CastAt = (a, pos) => { a.Activate(); };

    private bool systemEnabled = true;
    private int selectedIndex = -1;
    private string stickyId = null;
    private Camera cam;

    void Awake()
    {
        cam = Camera.main;
        systemEnabled = PlayerPrefs.GetInt("SelectionSystemEnabled", 1) == 1;
    }

    void OnEnable()
    {
        if (prevCardAction != null)
        {
            prevCardAction.action.Enable();
            prevCardAction.action.performed += OnPrev;         
        }
        if (nextCardAction != null)
        {
            nextCardAction.action.Enable();
            nextCardAction.action.performed += OnNext;       
        }
        if (smartcastAction != null)
        {
            smartcastAction.action.Enable();
            smartcastAction.action.started += OnSmartStart;   
            smartcastAction.action.canceled += OnSmartEnd;     
        }

        if (handUI != null) handUI.OnHandChanged += HandleHandChanged;
    }

    void OnDisable()
    {
        if (prevCardAction != null)
        {
            prevCardAction.action.performed -= OnPrev;
            prevCardAction.action.Disable();
        }
        if (nextCardAction != null)
        {
            nextCardAction.action.performed -= OnNext;
            nextCardAction.action.Disable();
        }
        if (smartcastAction != null)
        {
            smartcastAction.action.started -= OnSmartStart;
            smartcastAction.action.canceled -= OnSmartEnd;
            smartcastAction.action.Disable();
        }

        if (handUI != null) handUI.OnHandChanged -= HandleHandChanged;
    }



    void Start()
    {
        if (!systemEnabled) { ApplyDisabledState(); return; }
        SelectIndex(FindFirstNonNull(0));
        UpdateNameLabel();
    }

    void Update()
    {
        if (!systemEnabled) return;

        Vector2 mouse = GetMouseWorld();
        if (indicator != null) indicator.UpdateNamePosition(mouse);
        if (indicator != null && indicator.IsPreviewShown) indicator.UpdatePreviewAim(mouse);
    }

    private bool CanCastAbility(Ability a)
    {
        if (a == null) return false;

        // read a.manaCost via reflection; default 0 if missing
        float cost = 0f;
        var t = a.GetType();

        var f = t.GetField("manaCost");
        if (f != null) cost = Convert.ToSingle(f.GetValue(a));

        var p = t.GetProperty("ManaCost");
        if (p != null) cost = Convert.ToSingle(p.GetValue(a, null));

        var pm = PlayerMana.Instance;
        if (pm == null) return true; // allow casting if there is no mana system
        return pm.GetMana() >= cost;
    }

    public void SetSelectionSystemEnabled(bool on)
    {
        systemEnabled = on;
        if (!on) ApplyDisabledState();
        else
        {
            SelectIndex(FindFirstNonNull(Mathf.Max(0, selectedIndex)));
            if (indicator != null) { indicator.SetNameVisible(true); UpdateNameLabel(); }
        }
    }

    private void ApplyDisabledState()
    {
        if (handUI != null) handUI.SetSelectedIndex(-1);
        if (indicator != null) { indicator.HidePreview(); indicator.SetNameVisible(false); }
    }

    private void OnPrev(InputAction.CallbackContext ctx)
    {
        if (!systemEnabled) return;
        bool skipNoMana = Keyboard.current != null && Keyboard.current.shiftKey.isPressed;
        MoveSelection(-1, skipNoMana);
    }
    private void OnNext(InputAction.CallbackContext ctx)
    {
        if (!systemEnabled) return;
        bool skipNoMana = Keyboard.current != null && Keyboard.current.shiftKey.isPressed;
        MoveSelection(+1, skipNoMana);
    }

    private void MoveSelection(int dir, bool skipNoMana)
    {
        if (handUI == null) return;
        int count = handUI.HandLength; if (count == 0) return;

        int i = selectedIndex < 0 ? 0 : selectedIndex;
        for (int step = 0; step < count; step++)
        {
            i = Wrap(i + dir, count);
            var a = handUI.GetAbilityAt(i);
            if (a == null) continue;
            if (skipNoMana && !CanCastAbility(a)) continue;
            SelectIndex(i);
            return;
        }
    }

    private void OnSmartStart(InputAction.CallbackContext ctx)
    {
        if (!systemEnabled) return;
        if (selectedIndex < 0) return;
        var a = handUI != null ? handUI.GetAbilityAt(selectedIndex) : null;
        if (a == null || indicator == null) return;
        indicator.ShowPreviewFor(a);
        indicator.UpdatePreviewAim(GetMouseWorld());
    }
    private void OnSmartEnd(InputAction.CallbackContext ctx)
    {
        if (indicator != null) indicator.HidePreview();          // hide the preview
        if (handUI != null) handUI.UseCardFromSelection(selectedIndex);  // shows "Not enough mana!" if needed
    }

    private void HandleHandChanged()
    {
        if (!systemEnabled || handUI == null) return;

        if (stickySelection && !string.IsNullOrEmpty(stickyId))
        {
            int idx = handUI.FindIndexByAbilityId(stickyId);
            if (idx != -1) { SelectIndex(idx); return; }
        }

        int fallback = NearestNeighborFrom(Mathf.Max(0, selectedIndex));
        SelectIndex(fallback);
    }

    private void SelectIndex(int index)
    {
        if (handUI == null)
        {
            selectedIndex = -1;
            stickyId = null;
            return;
        }

        if (index < 0)
        {
            selectedIndex = -1;
            handUI.SetSelectedIndex(-1); // clears highlight in UI
            stickyId = null;
            UpdateNameLabel();           // hides name
            return;
        }

        selectedIndex = Mathf.Clamp(index, 0, Mathf.Max(0, handUI.HandLength - 1));
        var a = handUI.GetAbilityAt(selectedIndex);

        if (a == null)
        {
            selectedIndex = -1;
            handUI.SetSelectedIndex(-1);
            stickyId = null;
            UpdateNameLabel();
            return;
        }

        handUI.SetSelectedIndex(selectedIndex);
        stickyId = GetAbilityId(a);
        UpdateNameLabel();
    }
    private void UpdateNameLabel()
    {
        if (indicator == null) return;
        var a = handUI != null ? handUI.GetAbilityAt(selectedIndex) : null;
        if (a != null)
        {
            indicator.SetNameVisible(true);
            indicator.SetNameText(CleanAbilityName(a.name));
        }
        else indicator.SetNameVisible(false);
    }

    private string CleanAbilityName(string n)
    {
        if (string.IsNullOrEmpty(n)) return "";
        const string clone = "(Clone)";
        if (n.EndsWith(clone, StringComparison.Ordinal))
            return n.Substring(0, n.Length - clone.Length).Trim();
        return n;
    }

    private int NearestNeighborFrom(int from)
    {
        int count = handUI.HandLength; if (count == 0) return 0;
        for (int d = 0; d < count; d++)
        {
            int r = Wrap(from + d, count); if (handUI.GetAbilityAt(r) != null) return r;
            int l = Wrap(from - d, count); if (handUI.GetAbilityAt(l) != null) return l;
        }
        return -1;
    }
    private int FindFirstNonNull(int start)
    {
        int count = handUI != null ? handUI.HandLength : 0;
        for (int i = 0; i < count; i++)
        {
            int idx = Wrap(start + i, count);
            if (handUI.GetAbilityAt(idx) != null) return idx;
        }
        return -1;
    }
    private static int Wrap(int i, int count)
    {
        if (count <= 0) return 0;
        if (i < 0) i += ((-i / count) + 1) * count;
        return i % count;
    }
    private Vector2 GetMouseWorld()
    {
        Vector2 m = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        var p = cam != null ? cam.ScreenToWorldPoint(new Vector3(m.x, m.y, -cam.transform.position.z)) : Vector3.zero;
        return new Vector2(p.x, p.y);
    }
    private string GetAbilityId(Ability a)
    {
        var t = a.GetType();
        var f = t.GetField("internalID") ?? t.GetField("abilityId") ?? t.GetField("id");
        if (f != null) { var v = f.GetValue(a); if (v != null) return v.ToString(); }
        var p = t.GetProperty("InternalID") ?? t.GetProperty("AbilityId") ?? t.GetProperty("Id");
        if (p != null) { var v = p.GetValue(a, null); if (v != null) return v.ToString(); }
        return a.name;
    }
}
