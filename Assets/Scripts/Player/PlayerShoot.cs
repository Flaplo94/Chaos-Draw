using UnityEngine;
using System.Collections;

public class PlayerShoot : MonoBehaviour
{
    [Header("Shooting Settings")]
    public GameObject projectilePrefab;
    public Transform shootPoint;
    public float baseAttackRate = 1f; // shots per second
    public float projectileSpeed = 10f;

    private float shootCooldown;
    private bool shootingEnabled = false;

    void Start()
    {
        EnableShooting(); // Skyder med det samme fra f�rste wave
    }

    void Update()
    {
        if (!shootingEnabled) return;

        shootCooldown -= Time.deltaTime;

        if (shootCooldown <= 0f)
        {
            GameObject target = FindClosestEnemy();
            if (target != null)
            {
                ShootAt(target.transform);
            }

            shootCooldown = 1f / (baseAttackRate * (1f + BuffSystem.Instance.GetBuffLevel("AttackSpeed") * 0.1f));
        }
    }

    public void EnableShooting()
    {
        shootingEnabled = true;
    }

    void ShootAt(Transform target)
    {
        if (shootPoint == null || projectilePrefab == null || target == null)
        {
            Debug.LogWarning("Missing references in ShootAt()");
            return;
        }

        int projectileCount = 1 + BuffSystem.Instance.GetBuffLevel("ExtraProj");
        float spreadAngle = 10f;

        for (int i = 0; i < projectileCount; i++)
        {
            float angleOffset = (i - projectileCount / 2f) * spreadAngle;

            Vector2 direction = (target.position - shootPoint.position).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + angleOffset;
            Quaternion rotation = Quaternion.Euler(0, 0, angle);

            Vector3 spawnPosition = shootPoint.position + (Vector3)(direction * 0.5f);

            GameObject proj = Instantiate(projectilePrefab, spawnPosition, rotation);
            Rigidbody2D rb = proj.GetComponent<Rigidbody2D>();
            Collider2D projCol = proj.GetComponent<Collider2D>();
            Collider2D playerCol = GetComponent<Collider2D>();

            if (rb != null)
            {
                rb.linearVelocity = direction * projectileSpeed;
            }

            if (projCol != null && playerCol != null)
            {
                Physics2D.IgnoreCollision(projCol, playerCol);
            }

            proj.transform.rotation = rotation;

            Projectile projectile = proj.GetComponent<Projectile>();
            if (projectile != null)
            {
                projectile.damage = 1 + BuffSystem.Instance.GetBuffLevel("Damage");
                projectile.explosive = BuffSystem.Instance.GetBuffLevel("Explosive") > 0;
            }
        }
    }

    GameObject FindClosestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        GameObject closest = null;
        float shortestDistance = Mathf.Infinity;

        foreach (GameObject enemy in enemies)
        {
            float dist = Vector2.Distance(transform.position, enemy.transform.position);
            if (dist < shortestDistance)
            {
                shortestDistance = dist;
                closest = enemy;
            }
        }

        return closest;
    }
}
