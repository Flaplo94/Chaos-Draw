using UnityEngine;

public enum ElementType { None, Fire, Cold, Lightning, Arcane, Physical }

public class PlayerStats : MonoBehaviour
{
    [Header("Damage multipliers")]
    public float damageMult = 1f;     // global
    public float fireDamageMult = 1f; // element-specific

    public float GetDamageMult(ElementType element)
    {
        float m = damageMult;
        if (element == ElementType.Fire) m *= fireDamageMult;
        return m;
    }
}
