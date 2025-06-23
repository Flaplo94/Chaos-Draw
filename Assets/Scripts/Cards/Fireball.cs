using UnityEngine;

public class Fireball : MonoBehaviour
{
    public float speed = 10f;
    public int damage = 10;
    public float aoeRadius = 2f;
    private Vector2 direction;

    public void SetDirection(Vector2 dir)
    {
        direction = dir.normalized;
    }

    void Update()
    {
        transform.position += (Vector3)direction * speed * Time.deltaTime;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Damage single enemy
        EnemyHealth enemy = other.GetComponent<EnemyHealth>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
            // AOE
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, aoeRadius);
            foreach (var hit in hits)
            {
                EnemyHealth aoeEnemy = hit.GetComponent<EnemyHealth>();
                if (aoeEnemy != null && aoeEnemy != enemy)
                    aoeEnemy.TakeDamage(damage);
            }
            Destroy(gameObject);
            return;
        }

        // Damage boss
        BossHealth boss = other.GetComponent<BossHealth>();
        if (boss != null)
        {
            boss.TakeDamage(damage);
            // AOE
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, aoeRadius);
            foreach (var hit in hits)
            {
                BossHealth aoeBoss = hit.GetComponent<BossHealth>();
                if (aoeBoss != null && aoeBoss != boss)
                    aoeBoss.TakeDamage(damage);
            }
            Destroy(gameObject);
            return;
        }
    }
}