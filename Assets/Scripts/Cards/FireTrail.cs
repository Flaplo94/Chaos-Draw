using UnityEngine;
using System.Collections;

public class FireTrail : MonoBehaviour, IAbilityBehavior
{
    [SerializeField] private GameObject firePrefab; // forventer FireOnGround på prefab
    [SerializeField] private float duration = 3f;
    [SerializeField] private float dropInterval = 0.3f;

    // Rarity scaling
    [SerializeField] private float radiusBonusPerRarity = 0.0f; // info til FireOnGround via Initialize
    [SerializeField] private int damageBonusPerRarity = 0;      // info til FireOnGround via Initialize

    private Transform player;
    private Rarity rarityApplied = Rarity.Common;

    public bool Initialize(Vector2 _, Rarity rarity)
    {
        rarityApplied = rarity;

        switch (rarity)
        {
            case Rarity.Uncommon:
                duration *= 1.15f;
                radiusBonusPerRarity = 0.15f;
                damageBonusPerRarity = 1;
                break;
            case Rarity.Rare:
                duration *= 1.25f;
                radiusBonusPerRarity = 0.25f;
                damageBonusPerRarity = 1;
                break;
            case Rarity.Epic:
                duration *= 1.35f;
                radiusBonusPerRarity = 0.35f;
                damageBonusPerRarity = 2;
                break;
            case Rarity.Legendary:
                duration *= 1.50f;
                radiusBonusPerRarity = 0.50f;
                damageBonusPerRarity = 3;
                break;
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
                if (fire.TryGetComponent(out IAbilityBehavior behavior))
                {
                    behavior.Initialize(Vector2.zero, rarityApplied);
                }
            }

            yield return new WaitForSeconds(dropInterval);
            timer += dropInterval;
        }

        Destroy(gameObject);
    }
}
