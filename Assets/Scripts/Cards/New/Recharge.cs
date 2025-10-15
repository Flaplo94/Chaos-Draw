using UnityEngine;

public class Recharge : MonoBehaviour, IAbilityBehavior
{
    [SerializeField] private int amount = 1; // +1 mana immediately

    public bool Initialize(Vector2 _, Rarity __)
    {
        var mana = FindFirstObjectByType<PlayerMana>();
        if (mana == null)
        {
            Debug.LogWarning("[Recharge] No PlayerMana found in scene.");
            Destroy(gameObject);
            return true; // still consume the card so it doesn't hang
        }

        mana.GainMana(amount);

        Destroy(gameObject);
        return true;
    }
}
