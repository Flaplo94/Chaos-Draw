using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class CardMiniPreviewUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI")]
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text quantityText; // assign 'Qty' TMP i prefab

    [Header("Data")]
    [SerializeField] private ScriptableObject data;

    [Header("Hover events (menu-tooltip)")]
    public UnityEvent<ScriptableObject> onHoverEnter;
    public UnityEvent onHoverExit;

    public void SetObject(ScriptableObject so, Sprite fallbackSprite)
    {
        data = so;
        if (icon != null)
        {
            icon.sprite = ResolveSprite(so) ?? fallbackSprite;
            icon.enabled = icon.sprite != null;
        }
        SetQuantity(1);
    }

    public void SetQuantity(int count)
    {
        if (quantityText == null) return;
        if (count > 1)
        {
            quantityText.gameObject.SetActive(true);
            quantityText.text = "x" + count.ToString();
        }
        else
        {
            quantityText.gameObject.SetActive(false);
        }
    }

    public void OnPointerEnter(PointerEventData _)
    {
        if (data != null) onHoverEnter?.Invoke(data);
    }

    public void OnPointerExit(PointerEventData _)
    {
        onHoverExit?.Invoke();
    }

    private Sprite ResolveSprite(ScriptableObject so)
    {
        if (so == null) return null;

        var cd = so as CardData;
        if (cd != null) return cd.Icon;

        var sprite = TryFindSpriteByCommonNames(so);
        if (sprite != null) return sprite;

        var abilityObj = GetMemberValue(so, "ability");
        if (abilityObj is ScriptableObject abilitySo)
        {
            var abilitySprite = TryFindSpriteByCommonNames(abilitySo);
            if (abilitySprite != null) return abilitySprite;
        }

        return null;
    }

    private Sprite TryFindSpriteByCommonNames(ScriptableObject so)
    {
        Type t = so.GetType();
        string[] names = {
            "icon","Icon","sprite","Sprite","art","Art","artwork","Artwork",
            "image","Image","cardSprite","CardSprite","illustration","Illustration"
        };

        for (int i = 0; i < names.Length; i++)
        {
            var f = t.GetField(names[i], BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (f != null && f.FieldType == typeof(Sprite))
            {
                var val = f.GetValue(so) as Sprite;
                if (val != null) return val;
            }

            var p = t.GetProperty(names[i], BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (p != null && p.PropertyType == typeof(Sprite))
            {
                var val = p.GetValue(so, null) as Sprite;
                if (val != null) return val;
            }
        }
        return null;
    }

    private object GetMemberValue(object obj, string name)
    {
        if (obj == null) return null;
        var t = obj.GetType();

        var f = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (f != null) return f.GetValue(obj);

        var p = t.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (p != null) return p.GetValue(obj, null);

        return null;
    }
}
