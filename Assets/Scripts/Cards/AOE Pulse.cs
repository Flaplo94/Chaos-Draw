using UnityEngine;

public class AOEPulse : MonoBehaviour
{
    [SerializeField] private float radius = 4f;
    [SerializeField] private int damage = 3;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private GameObject radiusVisual;
    [SerializeField] private Color aoeColor = Color.red; // Set in Inspector for each ability



    private void Start()
    {
        Collider2D[] enemies = Physics2D.OverlapCircleAll(transform.position, radius, enemyLayer);
        foreach (var enemy in enemies)
        {
            enemy.GetComponent<EnemyHealth>()?.TakeDamage(damage);
        }

        if (radiusVisual != null)
        {
            GameObject vfx = Instantiate(radiusVisual, transform.position, Quaternion.identity);
            vfx.transform.localScale = Vector3.one * radius * 2f; // Diameter
            var sr = vfx.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.color = aoeColor;
            Destroy(vfx, 0.2f); // Auto-remove visual
        }

        // Optional: play animation or flash
        Destroy(gameObject, 0.1f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
