using UnityEngine;
using System.Collections;

public class Flow : MonoBehaviour, IAbilityBehavior
{
    [SerializeField] private int charges = 2; // next 2 cards cost -1

    // Initialize is called BEFORE Ability.Activate() spends mana.
    // We defer granting charges by one frame so Flow doesn't discount itself.
    public bool Initialize(Vector2 _, Rarity __)
    {
        StartCoroutine(GrantChargesNextFrame());
        return true; // cast succeeds
    }

    private IEnumerator GrantChargesNextFrame()
    {
        // Wait one frame: Ability.Activate() will compute/spend cost this frame.
        yield return null;

        var pbm = PlayerBuffManager.Instance ?? FindFirstObjectByType<PlayerBuffManager>();
        if (pbm != null)
        {
            pbm.AddFlowCharges(charges); // now only the NEXT casts get -1
        }

        Destroy(gameObject); // clean up the effect prefab
    }
}
