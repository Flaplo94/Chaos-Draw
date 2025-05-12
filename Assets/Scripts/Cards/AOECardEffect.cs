using UnityEngine;

public class AOECardEffect : MonoBehaviour
{
    public float radius = 2f;

    void Start()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                Destroy(hit.gameObject); // Fjenden dør
            }
        }

        Destroy(gameObject, 0.5f); // Ryd op efter effekten
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
