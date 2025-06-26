using UnityEngine;

public class EnemyHealer : MonoBehaviour
{
    [SerializeField] private float healRange = 4f;
    [SerializeField] private float healAmount = 1f;
    [SerializeField] private float healCooldown = 5f;
    [Header("Healing Zone Visual")]
    [SerializeField] private LineRenderer ringRenderer;
    [SerializeField] private int segments = 50;

    
    private float timer;

    void Start()
    {
        DrawHealingRing();
    }

    private void DrawHealingRing()
    {
        if (ringRenderer == null) return;

        ringRenderer.startColor = Color.green;
        ringRenderer.endColor = Color.green;

        ringRenderer.positionCount = segments + 1;
        ringRenderer.loop = true;

        for (int i = 0; i <= segments; i++)
        {
            float angle = 2f * Mathf.PI * i / segments;
            float x = Mathf.Cos(angle) * healRange;
            float y = Mathf.Sin(angle) * healRange;
            ringRenderer.SetPosition(i, new Vector3(x, y, 0f));
        }
    }
    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            HealNearby();
            timer = healCooldown;
        }
    }

    private void HealNearby()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, healRange);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy") && hit.gameObject != gameObject)
            {
                EnemyHealth health = hit.GetComponent<EnemyHealth>();
                if (health != null && health.GetHealth() < health.GetMaxHealth())
                {
                    health.Heal((int)healAmount);
                }
            }
        }
    }
}
