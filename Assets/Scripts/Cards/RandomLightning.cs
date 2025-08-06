using UnityEngine;
using System.Collections.Generic;

public class RandomLightning : MonoBehaviour
{
    [SerializeField] private float radius = 6f;
    [SerializeField] private int minStrikes = 1;
    [SerializeField] private int maxStrikes = 2;
    [SerializeField] private int damage = 2;
    [SerializeField] private GameObject lightningEffect;

    private void Start()
    {
        GameObject[] allEnemies = GameObject.FindGameObjectsWithTag("Enemy");
        List<GameObject> validEnemies = new List<GameObject>();

        foreach (GameObject enemy in allEnemies)
        {
            float dist = Vector2.Distance(transform.position, enemy.transform.position);
            if (dist <= radius)
            {
                validEnemies.Add(enemy);
                Debug.Log("Enemy in range: " + enemy.name);
            }
        }

        if (validEnemies.Count == 0)
        {
            Debug.Log("RandomLightning: No enemies in range.");
            Destroy(gameObject);
            return;
        }

        int strikeCount = Mathf.Clamp(Random.Range(minStrikes, maxStrikes + 1), 0, validEnemies.Count);

        for (int i = 0; i < strikeCount; i++)
        {
            int index = Random.Range(0, validEnemies.Count);
            GameObject target = validEnemies[index];
            validEnemies.RemoveAt(index);

            var health = target.GetComponent<EnemyHealth>();
            if (health != null)
            {
                health.TakeDamage(damage);
                Debug.Log("RandomLightning: Damaged " + target.name);
            }

            if (lightningEffect != null)
                Instantiate(lightningEffect, target.transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}
