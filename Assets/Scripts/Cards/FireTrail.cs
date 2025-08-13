using UnityEngine;
using System.Collections;

public class FireTrail : MonoBehaviour, IAbilityBehavior
{
    [SerializeField] private GameObject firePrefab; // FireOnGround prefab
    [SerializeField] private float duration = 3f;
    [SerializeField] private float dropInterval = 0.3f;
    [SerializeField] private float radiusBonusPerRarity = 0.0f; // Extra radius scaling
    [SerializeField] private int damageBonusPerRarity = 0;      // Extra damage scaling

    private Transform player;
    private Rarity rarityApplied = Rarity.Common;

    // Rarity hook — called by Ability when spawning this controller
    public bool Initialize(Vector2 _, Rarity rarity)
    {
        rarityApplied = rarity;

        // These bonuses will be passed to FireOnGround via rarity scaling
        switch (rarity)
        {
            case Rarity.Uncommon:
                duration *= 1.15f;
                radiusBonusPerRarity = 0.15f; // +15% radius
                damageBonusPerRarity = 1;
                break;
            case Rarity.Rare:
                duration *= 1.25f;
                radiusBonusPerRarity = 0.25f; // +25% radius
                damageBonusPerRarity = 1;
                break;
            case Rarity.Epic:
                duration *= 1.35f;
                radiusBonusPerRarity = 0.35f; // +35% radius
                damageBonusPerRarity = 2;
                break;
            case Rarity.Legendary:
                duration *= 1.50f;
                radiusBonusPerRarity = 0.50f; // +50% radius
                damageBonusPerRarity = 3;
                break;
                // Common = no bonus
        }
        return true;
    }

    private void Start()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            Debug.LogWarning("FireTrail: Player not found.");
            Destroy(gameObject);
            return;
        }

        player = playerObj.transform;
        StartCoroutine(LeaveTrail());
    }

    private IEnumerator LeaveTrail()
    {
        float timer = 0f;

        while (timer < duration)
        {
            if (firePrefab != null && player != null)
            {
                GameObject fire = Instantiate(firePrefab, player.position, Quaternion.identity);

                // Forward rarity to FireOnGround so it scales radius/damagePerTick
                var behavior = fire.GetComponent<IAbilityBehavior>();
                if (behavior != null)
                {
                    behavior.Initialize(Vector2.zero, rarityApplied);
                }
            }

            yield return new WaitForSeconds(dropInterval);
            timer += dropInterval;
        }

        Destroy(gameObject); // remove the trail controller
    }
}
