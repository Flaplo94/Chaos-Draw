using UnityEngine;
using UnityEngine.EventSystems;

public class CardHoverTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Ability ability;

    public void SetAbility(Ability a)
    {
        ability = a;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log("[HoverTrigger] ENTER " + ability?.abilityName);

        if (ability != null && IsDimmerActive())
        {
            Debug.Log("[HoverTrigger] SHOW card: " + ability.abilityName);
            CardHoverUI.Instance.Show(ability, transform.position);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log("[HoverTrigger] EXIT " + ability?.abilityName);

        if (IsDimmerActive())
        {
            CardHoverUI.Instance.Hide();
        }
    }

    private bool IsDimmerActive()
    {
        var dim = FindObjectOfType<Dimmer>();
        if (dim == null) return false;

        var cg = dim.GetComponent<CanvasGroup>();
        if (cg == null) return false;

        return cg.alpha > 0.01f; // hover kun hvis dimmeren faktisk er synlig
    }
}
