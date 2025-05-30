using UnityEngine;

public class EnemyTestDamage : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            var allEnemies = GameObject.FindGameObjectsWithTag("Enemy");
            foreach (var enemy in allEnemies)
            {
                enemy.GetComponent<EnemyHealth>().TakeDamage(1);
            }
        }
    }
}
