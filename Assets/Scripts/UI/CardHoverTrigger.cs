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

        if (ability != null)
        {
            Debug.Log("[HoverTrigger] SHOW card: " + ability.abilityName);
            CardHoverUI.Instance.Show(ability, transform.position);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log("[HoverTrigger] EXIT " + ability?.abilityName);
        CardHoverUI.Instance.Hide();
    }
}
