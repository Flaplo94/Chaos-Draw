using UnityEngine;
using System.Collections.Generic;

public class RandomLightning : MonoBehaviour, IAbilityBehavior
{
    [SerializeField] private float radius = 6f;
    [SerializeField] private int minStrikes = 1;
    [SerializeField] private int maxStrikes = 2;
    [SerializeField] private int damage = 2;
    [SerializeField] private GameObject lightningEffect;

    // rarity scaling
    private int bonusStrikes = 0;
    private float damageMultiplier = 1f;

    // Called by Ability after Instantiate
    public void Initialize(Vector2 _, Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Uncommon:
                bonusStrikes = 1;
                damageMultiplier = 1.10f;
                break;
            case Rarity.Rare:
                bonusStrikes = 2;
                damageMultiplier = 1.20f;
                break;
            case Rarity.Epic:
                bonusStrikes = 3;
                damageMultiplier = 1.30f;
                break;
            case Rarity.Legendary:
                bonusStrikes = 4;
                damageMultiplier = 1.40f;
                break;
                // Common = baseline
        }
    }

    private void Start()
    {
        // collect enemies in radius
        GameObject[] allEnemies = GameObject.FindGameObjectsWithTag("Enemy");
        List<GameObject> validEnemies = new List<GameObject>(allEnemies.Length);

        foreach (GameObject enemy in allEnemies)
        {
            if (enemy == null) continue;
            float dist = Vector2.Distance(transform.position, enemy.transform.position);
            if (dist <= radius)
                validEnemies.Add(enemy);
        }

        if (validEnemies.Count == 0)
        {
            Destroy(gameObject);
            return;
        }

        // apply rarity bonuses
        int adjMin = Mathf.Max(0, minStrikes + bonusStrikes);
        int adjMax = Mathf.Max(adjMin, maxStrikes + bonusStrikes);
        int strikeCount = Mathf.Clamp(Random.Range(adjMin, adjMax + 1), 0, validEnemies.Count);
        int finalDamage = Mathf.Max(1, Mathf.RoundToInt(damage * damageMultiplier));

        // strike unique random targets
        for (int i = 0; i < strikeCount; i++)
        {
            int index = Random.Range(0, validEnemies.Count);
            GameObject target = validEnemies[index];
            validEnemies.RemoveAt(index);

            if (target == null) continue;

            var health = target.GetComponent<EnemyHealth>();
            if (health != null)
                health.TakeDamage(finalDamage);

            if (lightningEffect != null)
                Instantiate(lightningEffect, target.transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}
