using UnityEngine;
using UnityEngine.EventSystems;

public class CardHoverTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Ability ability;

    /// <summary>
    /// Kaldes af CardSlotUI.Show() eller når et reward-kort bygges.
    /// </summary>
    public void SetAbility(Ability a)
    {
        ability = a;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (ability != null && IsDimmerActive())
        {
            CardHoverUI.Instance.Show(ability, transform.position);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (IsDimmerActive())
        {
            CardHoverUI.Instance.Hide();
        }
    }

    private bool IsDimmerActive()
    {
        var dim = FindFirstObjectByType<Dimmer>();
        if (dim == null) return false;

        var cg = dim.GetComponent<CanvasGroup>();
        if (cg == null) return false;

        return cg.alpha > 0.01f; // hover kun når dimmeren er synlig (pause, rewards, shop osv.)
    }
}
