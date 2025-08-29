using UnityEngine;

public class CardDamageHook : MonoBehaviour
{
    public float baseDamage = 10f;
    public ElementType element = ElementType.Physical;

    [HideInInspector] public float finalDamage = 10f;

    public void ApplyStats(PlayerStats stats)
    {
        float mult = (stats != null) ? stats.GetDamageMult(element) : 1f;
        finalDamage = baseDamage * mult;
    }
}
