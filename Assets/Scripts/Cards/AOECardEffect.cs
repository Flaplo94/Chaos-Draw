using UnityEngine;

public class AOECardEffect : MonoBehaviour
{
    public float radius = 2f;
    public LayerMask enemyLayer;

    private bool hasActivated = false;

    void Start()
    {
        Debug.Log("AOE spawned");

        // Vent 0.1 sek f�r vi aktiverer effekten, s� vi undg�r at den triggers �jeblikkeligt efter spawn
        Invoke(nameof(TriggerAOE), 0.1f);

        // �del�g dette objekt efter 1 sekund, s� det ikke bliver h�ngende
        Destroy(gameObject, 1f);
    }

    void TriggerAOE()
    {
        if (hasActivated) return;
        hasActivated = true;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius, enemyLayer);

        Debug.Log("AOE hits: " + hits.Length);

        foreach (var hit in hits)
        {
            if (hit != null && hit.CompareTag("Enemy") && hit.gameObject.activeInHierarchy)
            {
                Destroy(hit.gameObject);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
