using UnityEngine;

public class Projectile : MonoBehaviour
{
    public int damage = 1;
    public bool explosive = false;
    public float explosionRadius = 1.5f;
    public GameObject explosionEffect;

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Projectile collided with: " + other.name);

        if (other.CompareTag("Enemy"))
        {
            Debug.Log("Enemy hit!");
            DamageEnemy(other.gameObject);
            Destroy(gameObject);
        }
    }

    void DamageEnemy(GameObject enemy)
    {
        var health = enemy.GetComponent<EnemyHealth>();
        if (health != null)
        {
            health.TakeDamage(damage);
        }
    }
}
