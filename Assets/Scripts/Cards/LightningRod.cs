using UnityEngine;
using System.Collections.Generic;

public class LightningRod : MonoBehaviour, IAbilityBehavior
{
    [SerializeField] private float lifetime = 8f;
    [SerializeField] private float rodRange = 5f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float damageTickRate = 0.5f;
    [SerializeField] private LayerMask enemyLayer;              // inkluder Boss-lag
    [SerializeField] private LineRenderer lightningLinePrefab;

    private float damageTimer = 0f;
    private Transform player;
    private static List<LightningRod> activeRods = new List<LightningRod>();
    private List<LineRenderer> lines = new List<LineRenderer>();

    public bool Initialize(Vector2 _, Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Uncommon: rodRange *= 1.10f; damage *= 1.10f; damageTickRate *= 0.90f; break;
            case Rarity.Rare: rodRange *= 1.20f; damage *= 1.20f; damageTickRate *= 0.85f; break;
            case Rarity.Epic: rodRange *= 1.30f; damage *= 1.30f; damageTickRate *= 0.80f; break;
            case Rarity.Legendary: rodRange *= 1.40f; damage *= 1.40f; damageTickRate *= 0.70f; break;
        }
        if (damageTickRate < 0.05f) damageTickRate = 0.05f;
        return true;
    }

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        activeRods.Add(this);
        Invoke(nameof(DestroySelf), lifetime);
        damageTimer = 0f;
    }

    private void Update()
    {
        damageTimer -= Time.deltaTime;

        ClearLines();

        // linje til spiller (hvis indenfor rækkevidde)
        if (player != null && Vector2.Distance(player.position, transform.position) <= rodRange)
            DrawLightning(transform.position, player.position);

        // linjer til andre rods i rækkevidde
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
        line.useWorldSpace = true;
        if (line.positionCount < 2) line.positionCount = 2;
        line.SetPosition(0, from);
        line.SetPosition(1, to);
        lines.Add(line);

        // Damage på tick langs linjen
        if (damageTimer <= 0f)
        {
            var dir = (to - from).normalized;
            float len = Vector2.Distance(from, to);

            int finalTick = DamageCalculator.ComputeFinalDamage(Mathf.RoundToInt(damage), DamageElement.Lightning);

            RaycastHit2D[] hits = Physics2D.RaycastAll(from, dir, len, enemyLayer);
            foreach (var hit in hits)
            {
                if (hit.collider == null) continue;

                var eh = hit.collider.GetComponent<EnemyHealth>();
                if (eh != null) eh.TakeDamage(finalTick);

                var bh = hit.collider.GetComponent<BossHealth>();
                if (bh != null) bh.TakeDamage(finalTick);
            }

            damageTimer = damageTickRate;
        }

        Destroy(line.gameObject, Time.deltaTime); // kort “flicker”
    }

    private void ClearLines()
    {
        for (int i = 0; i < lines.Count; i++)
            if (lines[i] != null) Destroy(lines[i].gameObject);
        lines.Clear();
    }

    private void DestroySelf()
    {
        activeRods.Remove(this);
        Destroy(gameObject);
    }
}
