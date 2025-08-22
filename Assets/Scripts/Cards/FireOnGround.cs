using UnityEngine;

public class FireOnGround : MonoBehaviour, IAbilityBehavior
{
    [Header("Damage Area")]
    [SerializeField] private float radius = 2f;
    [SerializeField] private int damagePerTick = 1;
    [SerializeField] private float tickInterval = 0.5f;
    [SerializeField] private float duration = 3f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Tiled Fire VFX")]
    [Tooltip("Small fire animation prefab (Animator + SpriteRenderer). This is stamped many times to fill the circle.")]
    [SerializeField] private GameObject tilePrefab;
    [Tooltip("Distance between tiles in world units (lower = denser).")]
    [SerializeField] private float tileSpacing = 0.6f;
    [Tooltip("Random position jitter to break the grid look.")]
    [SerializeField] private float positionJitter = 0.12f;
    [Tooltip("Random Z-rotation for variety.")]
    [SerializeField] private bool randomizeRotation = true;
    [Tooltip("Animator Trigger to start the tile's spawn animation (leave empty if not used).")]
    [SerializeField] private string spawnTrigger = "Spawn";

    private float tickTimer;
    private float lifeTimer;

    // ---------- IAbilityBehavior ----------
    public bool Initialize(Vector2 dir, Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Uncommon: radius *= 1.2f; break;
            case Rarity.Rare: radius *= 1.4f; damagePerTick += 1; break;
            case Rarity.Epic: radius *= 1.6f; damagePerTick += 2; break;
            case Rarity.Legendary: radius *= 2.0f; damagePerTick += 3; break;
                // Common = baseline
        }
        return true;
    }

    private void Start()
    {
        tickTimer = 0f;
        lifeTimer = duration;

        SpawnTilesFillCircle();
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

            // 0 mask means "no filter", match your previous pattern.
            Collider2D[] hits = (enemyLayer.value == 0)
                ? Physics2D.OverlapCircleAll(transform.position, radius)
                : Physics2D.OverlapCircleAll(transform.position, radius, enemyLayer);

            foreach (var hit in hits)
            {
                if (!hit) continue;

                if (hit.TryGetComponent(out EnemyHealth eh)) eh.TakeDamage(damagePerTick);
                if (hit.TryGetComponent(out BossHealth bh)) bh.TakeDamage(damagePerTick);
            }
        }
    }

    private void SpawnTilesFillCircle()
    {
        if (!tilePrefab)
        {
            Debug.LogWarning("FireOnGround: tilePrefab not assigned. No visuals will spawn.");
            return;
        }

        // Simple square grid, culled by circle. You can tune tileSpacing in the inspector.
        // Loop from -radius..radius in both axes with step = tileSpacing.
        float r = radius * 0.98f; // slight inset to avoid edges poking out
        for (float x = -r; x <= r; x += tileSpacing)
        {
            for (float y = -r; y <= r; y += tileSpacing)
            {
                var offset = new Vector2(x, y);
                if (offset.sqrMagnitude > r * r) continue; // outside circle

                // Jitter to avoid obvious grid
                Vector2 jitter = Vector2.zero;
                if (positionJitter > 0f)
                {
                    jitter = new Vector2(
                        Random.Range(-positionJitter, positionJitter),
                        Random.Range(-positionJitter, positionJitter)
                    );
                }

                Vector3 pos = transform.position + (Vector3)(offset + jitter);
                var tile = Instantiate(tilePrefab, pos, Quaternion.identity, transform);

                if (randomizeRotation)
                {
                    float z = Random.Range(0f, 360f);
                    tile.transform.rotation = Quaternion.Euler(0f, 0f, z);
                }

                // Kick the tile's spawn animation if it has an Animator + trigger
                if (!string.IsNullOrEmpty(spawnTrigger))
                {
                    var anim = tile.GetComponentInChildren<Animator>();
                    if (anim)
                    {
                        anim.Rebind();
                        anim.Update(0f);
                        anim.ResetTrigger(spawnTrigger);
                        anim.SetTrigger(spawnTrigger);
                    }
                }
            }
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
#endif
}
