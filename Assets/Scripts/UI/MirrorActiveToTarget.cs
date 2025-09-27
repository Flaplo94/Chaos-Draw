using UnityEngine;

[DefaultExecutionOrder(-10)]
public class MirrorActiveToTarget : MonoBehaviour
{
    [SerializeField] private GameObject target;   // fx RewardUI
    [SerializeField] private bool inverse = false;

    [Header("Visibility")]
    [SerializeField] private bool useCanvasGroup = true;
    [SerializeField] private bool addCanvasGroupIfMissing = true;

    private CanvasGroup cg;
    private bool lastVisible;

    void Awake()
    {
        if (useCanvasGroup)
        {
            cg = GetComponent<CanvasGroup>();
            if (cg == null && addCanvasGroupIfMissing)
                cg = gameObject.AddComponent<CanvasGroup>();
        }
        Apply(ComputeVisible(), true);
    }

    void Update()
    {
        Apply(ComputeVisible(), false);
    }

    private bool ComputeVisible()
    {
        if (target == null) return false;
        bool v = target.activeInHierarchy;
        return inverse ? !v : v;
    }

    private void Apply(bool visible, bool force)
    {
        if (!force && visible == lastVisible) return;

        if (useCanvasGroup && cg != null)
        {
            cg.alpha = visible ? 1f : 0f;
            cg.interactable = visible;
            cg.blocksRaycasts = visible;
        }
        else
        {
            // Virker kun hvis dette object starter aktivt.
            if (gameObject.activeSelf != visible)
                gameObject.SetActive(visible);
        }

        if (visible)
        {
            var ctrl = GetComponent<CardRewardRerollController>();
            if (ctrl != null) ctrl.RefreshUI();
        }

        lastVisible = visible;
    }
}
