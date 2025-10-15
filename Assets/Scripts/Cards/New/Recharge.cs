using UnityEngine;

public class Recharge : MonoBehaviour
{
    [SerializeField] private int amount = 1; // +1 temp mana (tweakable if you ever want +2, etc.)

    void Start()
    {
        var mana = FindFirstObjectByType<PlayerMana>();
        if (mana == null)
        {
            Debug.LogWarning("[Recharge] No PlayerMana found in scene.");
            Destroy(gameObject);
            return;
        }

        // Assumes your mana system has a temp-mana API like this:
        mana.GainMana(amount);

        // Done instantly; no lingering object
        Destroy(gameObject);
    }
}
