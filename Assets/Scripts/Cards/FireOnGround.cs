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
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private float tileSpacing = 0.6f;
    [SerializeField] private float positionJitter = 0.12f;
    [SerializeField] private bool randomizeRotation = true;
    [SerializeField] private string spawnTrigger = "Spawn";

    [Header("Lucky Shot (independent forward offset)")]
    [Tooltip("Minimum forward distance (world units) to place the duplicate in aim direction.")]
    [SerializeField] private float duplicateForwardOffsetMin = 1.5f;
    [Tooltip("Forward distance scales with radius (Final = max(Min, radius * Scale)).")]
    [SerializeField] private float duplicateForwardOffsetScale = 1.20f;

    private float tickTimer;
    private float lifeTimer;
    private bool luckyWasDuplicated = false;

    // store aim so we can place the duplicate forward
    private Vector2 castDir = Vector2.right;

    public bool Initialize(Vector2 dir, Rarity rarity)
    {
        // keep aim for forward placement
        castDir = (dir.sqrMagnitude > 0.0001f) ? dir.normalized : Vector2.right;

        switch (rarity)
        {
            case Rarity.Uncommon: radius *= 1.2f; damagePerTick += 1; break;
            case Rarity.Rare: radius *= 1.4f; damagePerTick += 2; break;
            case Rarity.Epic: radius *= 1.6f; damagePerTick += 3; break;
            case Rarity.Legendary: radius *= 2.0f; damagePerTick += 4; break;
        }
        return true;
    }

    private void Start()
    {
        tickTimer = 0f;
        lifeTimer = duration;
        SpawnTilesFillCircle();

        // Lucky Shot duplicate — forward in aim direction with a BIGGER, radius-scaled offset
        if (!luckyWasDuplicated)
        {
            LuckyShotSystem.OnSpellCast(this, () =>
            {
                Vector2 fwd = (castDir.sqrMagnitude > 0.0001f) ? castDir : Vector2.right;

                float forwardDist = Mathf.Max(duplicateForwardOffsetMin, radius * duplicateForwardOffsetScale);
                Vector3 p2 = transform.position + (Vector3)(fwd * forwardDist);

                var dup = Instantiate(gameObject, p2, transform.rotation);
                var comp = dup.GetComponent<FireOnGround>();
                if (comp != null)
                {
                    comp.luckyWasDuplicated = true; // prevent chaining
                    comp.castDir = this.castDir;    // keep same aim for consistent forward direction
                }
            });
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
            Collider2D[] hits = (enemyLayer.value == 0)
                ? Physics2D.OverlapCircleAll(transform.position, radius)
                : Physics2D.OverlapCircleAll(transform.position, radius, enemyLayer);

            foreach (var hit in hits)
            {
                if (!hit) continue;
                var result = DamageCalculator.ComputeFinalDamage(damagePerTick, DamageElement.Fire);
                if (hit.TryGetComponent(out EnemyHealth eh)) eh.TakeDamage(result.amount, result.element);
                if (hit.TryGetComponent(out BossHealth bh)) bh.TakeDamage(result.amount, result.element);
            }
        }
    }

    private void SpawnTilesFillCircle()
    {
        if (!tilePrefab) return;

        float r = radius * 0.98f;
        for (float x = -r; x <= r; x += tileSpacing)
        {
            for (float y = -r; y <= r; y += tileSpacing)
            {
                var offset = new Vector2(x, y);
                if (offset.sqrMagnitude > r * r) continue;

                Vector2 jitter = new Vector2(
                    Random.Range(-positionJitter, positionJitter),
                    Random.Range(-positionJitter, positionJitter)
                );

                Vector3 pos = transform.position + (Vector3)(offset + jitter);
                var tile = Instantiate(tilePrefab, pos, Quaternion.identity, transform);

                if (randomizeRotation)
                {
                    float z = Random.Range(0f, 360f);
                    tile.transform.rotation = Quaternion.Euler(0f, 0f, z);
                }

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
