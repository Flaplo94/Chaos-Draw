using UnityEngine;
using System.Collections.Generic;

public class LightningRod : MonoBehaviour
{
    [SerializeField] private float lifetime = 8f;
    [SerializeField] private float rodRange = 5f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float damageTickRate = 0.5f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private LineRenderer lightningLinePrefab;

    private float damageTimer = 0f;
    private Transform player;
    private static List<LightningRod> activeRods = new List<LightningRod>();
    private List<LineRenderer> lines = new List<LineRenderer>();

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        activeRods.Add(this);
        Invoke(nameof(DestroySelf), lifetime);
    }

    private void Update()
    {
        ClearLines();

        if (player != null && Vector2.Distance(player.position, transform.position) <= rodRange)
            DrawLightning(transform.position, player.position);

        foreach (var other in activeRods)
        {
            if (other == this) continue;
            float dist = Vector2.Distance(other.transform.position, transform.position);
            if (dist <= rodRange)
                DrawLightning(transform.position, other.transform.position);
        }
    }

    private void DrawLightning(Vector2 from, Vector2 to)
    {
        LineRenderer line = Instantiate(lightningLinePrefab);
        line.SetPosition(0, from);
        line.SetPosition(1, to);
        lines.Add(line);

        RaycastHit2D[] hits = Physics2D.RaycastAll(from, (to - from).normalized, Vector2.Distance(from, to), enemyLayer);
        damageTimer -= Time.deltaTime;
        if (damageTimer <= 0f)
        {
            damageTimer = damageTickRate;

            foreach (var hit in hits)
            {
                hit.collider.GetComponent<EnemyHealth>()?.TakeDamage((int)damage);
            }
        }

        Destroy(line.gameObject, Time.deltaTime); // Quick flicker effect
    }

    private void ClearLines()
    {
        foreach (var line in lines)
            if (line != null) Destroy(line.gameObject);

        lines.Clear();
    }

    private void DestroySelf()
    {
        activeRods.Remove(this);
        Destroy(gameObject);
    }
}