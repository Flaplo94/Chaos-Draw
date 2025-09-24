using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class StatBoxUpdater : MonoBehaviour
{
    [SerializeField, Tooltip("Realtids-interval i sekunder (0.1 = 10 Hz)")]
    private float updateInterval = 0.1f;

    private StatValueBinder[] _binders;
    private WaitForSecondsRealtime _wait;

    void Awake()
    {
        _binders = GetComponentsInChildren<StatValueBinder>(true);
        _wait = new WaitForSecondsRealtime(Mathf.Max(0.02f, updateInterval));
    }

    void OnEnable()
    {
        ForceRefresh();
        StartCoroutine(Loop());

        // subscribe til buff events for instant refresh
        if (PlayerBuffManager.Instance != null)
            PlayerBuffManager.Instance.OnValuesChanged += HandleBuffsChanged;
    }

    void OnDisable()
    {
        if (PlayerBuffManager.Instance != null)
            PlayerBuffManager.Instance.OnValuesChanged -= HandleBuffsChanged;
    }

    private void HandleBuffsChanged() => ForceRefresh();

    IEnumerator Loop()
    {
        while (enabled)
        {
            foreach (var b in _binders)
                if (b) b.Refresh();

            yield return _wait; // unscaled  UI tikker også i pause
        }
    }

    [ContextMenu("Force Refresh")]
    public void ForceRefresh()
    {
        if (_binders == null || _binders.Length == 0)
            _binders = GetComponentsInChildren<StatValueBinder>(true);

        foreach (var b in _binders)
            if (b) b.Refresh();
    }
}
