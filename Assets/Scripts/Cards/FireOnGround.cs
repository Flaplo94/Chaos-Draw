using UnityEngine;

public class FireOnGround : MonoBehaviour
{
    [SerializeField] private float radius = 2f;
    [SerializeField] private int damagePerTick = 1;
    [SerializeField] private float tickInterval = 0.5f;
    [SerializeField] private float duration = 3f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private GameObject fireVisual;
    [SerializeField] private Color fireColor = new Color(1f, 0.5f, 0f, 0.7f);

    private float tickTimer;
    private float lifeTimer;

    private void Start()
    {
        tickTimer = 0f;
        lifeTimer = duration;

        // Visual
        if (fireVisual != null)
        {
            GameObject vfx = Instantiate(fireVisual, transform.position, Quaternion.identity, transform);
            vfx.transform.localScale = Vector3.one * radius * 2f;
            var sr = vfx.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.color = fireColor;
        }
    }

    private void Update()
    {
        lifeTimer -= Time.deltaTime;
        tickTimer -= Time.deltaTime;

        if (lifeTimer <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        if (tickTimer <= 0f)
        {
            tickTimer = tickInterval;
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius, enemyLayer);
            foreach (var hit in hits)
            {
                var eh = hit.GetComponent<EnemyHealth>();
                if (eh != null)
                    eh.TakeDamage(damagePerTick);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}

