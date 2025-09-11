using System;
using System.Reflection;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class StatValueBinder : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text valueText;   // Finder selv barnet "Value" hvis tomt

    [Header("Kilde")]
    [SerializeField] private UnityEngine.Object target;  // Autofyldes med StatProbe fra forældre
    [SerializeField] private string member;              // Felt/property/metode (uden parametre)

    [Header("Format")]
    [SerializeField] private float multiplyBy = 1f;
    [SerializeField] private string suffix = "";
    [SerializeField] private string format = "{0:0.#}";

    [Header("Auto-wiring")]
    [SerializeField] private bool autoFindStatProbeInParents = true;

    // cache
    private MemberInfo _mi;
    private MethodInfo _method;
    private Type _type;

    void Awake()
    {
        // Find Value-tekst hvis ikke sat
        if (!valueText)
        {
            var t = transform.Find("Value");
            if (t) valueText = t.GetComponent<TMP_Text>();
        }

        // Find StatProbe på forældre-hierarkiet
        if (autoFindStatProbeInParents && target == null)
        {
            var probe = GetComponentInParent<StatProbe>(true);
            if (probe) target = probe;
        }

        CacheMember();
        Refresh();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!Application.isPlaying)
        {
            if (!valueText)
            {
                var t = transform.Find("Value");
                if (t) valueText = t.GetComponent<TMP_Text>();
            }

            if (autoFindStatProbeInParents && target == null)
            {
                var probe = GetComponentInParent<StatProbe>(true);
                if (probe) target = probe;
            }
        }
    }
#endif

    private void CacheMember()
    {
        _mi = null; _method = null;
        _type = target ? target.GetType() : null;
        if (_type == null || string.IsNullOrWhiteSpace(member)) return;

        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        var f = _type.GetField(member, flags);
        if (f != null) { _mi = f; return; }

        var p = _type.GetProperty(member, flags);
        if (p != null) { _mi = p; return; }

        var m = _type.GetMethod(member, flags, null, Type.EmptyTypes, null);
        if (m != null) { _method = m; return; }

        Debug.LogWarning($"[StatValueBinder] '{name}': Kan ikke finde '{member}' på {_type?.Name}.", this);
    }

    public void Refresh()
    {
        if (!valueText) return;

        if (target && _type != target.GetType())
            CacheMember();

        string txt = "—";
        try
        {
            object raw = null;
            if (target)
            {
                if (_method != null) raw = _method.Invoke(target, null);
                else if (_mi is FieldInfo fi) raw = fi.GetValue(target);
                else if (_mi is PropertyInfo pi) raw = pi.GetValue(target, null);
            }

            if (raw != null)
            {
                if (raw is IConvertible)
                {
                    double num = Convert.ToDouble(raw) * multiplyBy;
                    txt = string.Format(string.IsNullOrEmpty(format) ? "{0}" : format, num);
                }
                else txt = raw.ToString();
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[StatValueBinder] '{name}' kunne ikke læse '{member}': {e.Message}", this);
        }

        if (!string.IsNullOrEmpty(suffix)) txt += suffix;
        valueText.text = txt;
    }

    public void SetTarget(UnityEngine.Object newTarget, string newMember)
    {
        target = newTarget;
        member = newMember;
        CacheMember();
        Refresh();
    }
}
