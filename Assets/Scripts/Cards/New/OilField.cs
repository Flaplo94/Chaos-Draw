using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(Collider2D))]
public class OilField : MonoBehaviour, IAbilityBehavior
{
    [Header("Oil settings")]
    [Range(0f, 1f)] public float slowMultiplier = 0.40f; // 60% slow
    public float slowDuration = 3f;

    [Header("Ignition")]
    public float burnDuration = 5f;
    [Tooltip("Optional: change tint when burning")]
    public SpriteRenderer oilSprite;
    public Animator fireAnimator; // Animator instead of ParticleSystem
    public string fireAnimationName = "Fire"; // name of your fire animation
    public Color burningTint = new Color(1f, 0.6f, 0.2f, 1f);

    [Header("Detection")]
    public LayerMask enemyLayers;

    private bool isBurning = false;
    private Collider2D trigger;
    private readonly Dictionary<TimeBubbleEffector, float> _oilExpiry
    = new Dictionary<TimeBubbleEffector, float>();

    [Header("Range")]
    [Min(0.1f)] public float radius = 2f;             // editable in Inspector (world units)
    [SerializeField] private bool matchSpriteAndCollider = true;
    [SerializeField] private CircleCollider2D circle; // preferred

    [Header("Fire DoT")]
    [SerializeField] private float burnDps = 12f;        // damage per second while burning
    [SerializeField] private float burnTick = 0.25f;

    [Header("Fire Tiling (no scaling)")]
    [SerializeField] private GameObject fireTilePrefab; // a small 1×1 (or whatever) sprite+Animator that plays the fire anim on loop
    [SerializeField, Min(0.1f)] private float tilesPerUnit2 = 1.1f; // density: tiles per m² (adjust to taste)
    [SerializeField, Range(0f, 0.5f)] private float jitter = 0.15f; // random offset per tile (meters)
    [SerializeField, Min(1)] private int maxTiles = 150;            // safety cap
    private readonly List<GameObject> _spawnedFireTiles = new();

    [SerializeField, Range(0.1f, 2f)] private float fireTileScale = 1f;

    // use Time.time so we don't depend on frame counting
    private float Now => Time.time;

    private HashSet<GameObject> _inside = new HashSet<GameObject>();

    private enum DamageElementProxy { Fire, Physical, Lightning }

    public bool Initialize(Vector2 _, Rarity __)
    {
        var rb = GetComponent<Rigidbody2D>();
        if (!rb) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.simulated = true;
        rb.interpolation = RigidbodyInterpolation2D.None;

        if (!circle) circle = GetComponent<CircleCollider2D>();
        if (circle) circle.isTrigger = true;

        ApplyRadius();
        return true;
    }

    private void Awake()
    {
        // cache colliders if not assigned
        if (!circle) circle = GetComponent<CircleCollider2D>();
        
    }

    private void Start()
    {
        if (matchSpriteAndCollider) ApplyRadius();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying && matchSpriteAndCollider)
            ApplyRadius();
    }
    private void Update()
    {
        if (_oilExpiry.Count == 0) return;

        // collect expired first (avoid editing dictionary while iterating)
        System.Collections.Generic.List<TimeBubbleEffector> toRemove = null;

        foreach (var kv in _oilExpiry)
        {
            if (kv.Key == null || kv.Value <= Now)
            {
                (toRemove ??= new List<TimeBubbleEffector>()).Add(kv.Key);
            }
        }

        if (toRemove != null)
        {
            for (int i = 0; i < toRemove.Count; i++)
            {
                var eff = toRemove[i];
                if (eff) eff.RemoveSource(this);   // remove *our* oil source only
                _oilExpiry.Remove(eff);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isBurning)
        {
            var eh = GetEnemyHealth(other);
            if (eh) _inside.Add(eh.gameObject);
            return;
        }

        if (IsFireSource(other)) { Ignite(); return; }
        if (IsEnemy(other)) ApplyOrRefreshSlow(other.attachedRigidbody ? other.attachedRigidbody.gameObject : other.gameObject);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (isBurning)
        {
            var eh = GetEnemyHealth(other);
            if (eh) _inside.Add(eh.gameObject);
            return;
        }

        if (IsEnemy(other)) ApplyOrRefreshSlow(other.attachedRigidbody ? other.attachedRigidbody.gameObject : other.gameObject);
        if (IsFireSource(other)) Ignite();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!isBurning) return;
        var eh = GetEnemyHealth(other);
        if (eh) _inside.Remove(eh.gameObject);
    }


    private void ApplyRadius()
    {
        float targetWorldRadius = radius;
        float targetWorldDiameter = targetWorldRadius * 2f;

        // --- 1) Make collider world radius == radius (compensate for lossyScale) ---
        if (circle)
        {
            float maxScale = Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
            circle.isTrigger = true;
            circle.radius = (maxScale > 0.0001f) ? (targetWorldRadius / maxScale) : targetWorldRadius;
        }
        

        // --- 2) Make sprite world diameter == radius*2 (compensate for parent scaling & PPU) ---
        if (oilSprite && oilSprite.sprite)
        {
            Transform t = oilSprite.transform;

            // parent (world) scale factor that multiplies the child's localScale
            float parentScaleX = (t.localScale.x == 0f) ? 1f : t.lossyScale.x / t.localScale.x;
            float parentScaleY = (t.localScale.y == 0f) ? 1f : t.lossyScale.y / t.localScale.y;

            // sprite world size when oilSprite.localScale == (1,1,1)
            float ppu = oilSprite.sprite.pixelsPerUnit;
            Vector2 pxSize = oilSprite.sprite.rect.size;                // in pixels
            float baseWorldW = (pxSize.x / ppu) * parentScaleX;         // world width at localScale 1
            float baseWorldH = (pxSize.y / ppu) * parentScaleY;         // world height at localScale 1
            float baseWorldMax = Mathf.Max(baseWorldW, baseWorldH);

            if (baseWorldMax > 0.0001f)
            {
                float uniformLocal = targetWorldDiameter / baseWorldMax;
                t.localScale = new Vector3(uniformLocal, uniformLocal, 1f);
            }
        }

        // --- 3) Keep fire animator matched to sprite (if it’s a different object) ---
        if (fireAnimator)
        {
            if (oilSprite && fireAnimator.transform != oilSprite.transform)
                fireAnimator.transform.localScale = oilSprite.transform.localScale;
        }
    }


    // -------- Slow logic --------
    private void ApplyOrRefreshSlow(GameObject target)
    {
        if (!target) return;

        // 60% slow means multiplier = 0.40f (i.e., they move at 40% speed)
        const float OIL_MULT = 0.40f;

        var eff = target.GetComponent<TimeBubbleEffector>();
        if (!eff) eff = target.AddComponent<TimeBubbleEffector>();

        // add/update OUR source on that effector (non-compounding; Effector handles combining)
        eff.AddSource(this, OIL_MULT);

        // refresh linger window to 3s from now (or whatever your slowDuration is)
        float expireAt = Now + Mathf.Max(0.01f, slowDuration); // slowDuration = 3f in your inspector
        _oilExpiry[eff] = expireAt;
    }
    private bool IsEnemy(Collider2D c)
    {
        if (!c) return false;
        if (((1 << c.gameObject.layer) & enemyLayers.value) != 0) return true;
        if (c.CompareTag("Enemy")) return true;
        if (!c.CompareTag("Player") && c.GetComponent<Rigidbody2D>() != null) return true;
        return false;
    }

    // -------- Ignition detection --------
    private bool IsFireSource(Collider2D c)
    {
        if (!c) return false;

        // If the collider belongs to another OilField:
        var otherOil = c.GetComponentInParent<OilField>();
        if (otherOil != null)
        {
            // Don't self-ignite and don't ignite from unlit oil
            if (otherOil == this) return false;
            return otherOil.isBurning; // chain-ignite only from burning oil fields
        }

        // Fire-tagged abilities/projectiles
        if (c.CompareTag("Fire")) return true;

        // No loose name checks — prevents false positives
        return false;
    }

    private void ChainIgniteNeighbors()
    {
        if (!circle) circle = GetComponent<CircleCollider2D>();
        if (!circle) return;

        var filter = new ContactFilter2D
        {
            useTriggers = true,
            useLayerMask = false // check all layers; this is just for OilField neighbors
        };

        var results = new List<Collider2D>(32);
        int count = circle.Overlap(filter, results);
        if (count <= 0) return;

        for (int i = 0; i < results.Count; i++)
        {
            var col = results[i];
            if (!col) continue;

            var other = col.GetComponentInParent<OilField>();
            if (!other) continue;
            if (other == this) continue;
            if (other.isBurning) continue;

            // Only ignite if we actually overlap (defensive if collider shapes differ)
            other.IgniteFromNeighbor();
        }
    }
    internal void IgniteFromNeighbor()
    {
        // same as Ignite(), but callable from other OilFields
        Ignite();
    }

    private IEnumerator ChainIgniteNeighborsNextFrame()
    {
        yield return null;          // let physics update once
        ChainIgniteNeighbors();     // catch any neighbors that were overlapping already
    }

    private void Ignite()
    {
        if (isBurning) return;
        isBurning = true;

        // stop slowing immediately: remove our slow sources from all tracked targets
        foreach (var kv in _oilExpiry)            // <-- your existing dictionary used to track linger
            if (kv.Key) kv.Key.RemoveSource(this);
        _oilExpiry.Clear();

        // VISUALS: hide oil circle, show fire animator covering same area
        if (oilSprite) oilSprite.enabled = false;

        SpawnFireTiles();

        ChainIgniteNeighbors();
        StartCoroutine(ChainIgniteNeighborsNextFrame());

        // Begin burning damage over time for burnDuration
        StartCoroutine(BurnRoutine());
    }

    private IEnumerator BurnRoutine()
    {
        float t = 0f;
        float acc = 0f;

        // ensure collider sizing is up to date (optional if you already do this elsewhere)
        ApplyRadius();

        while (t < burnDuration)
        {
            float dt = Time.deltaTime;
            t += dt;
            acc += dt;

            // process damage ticks at fixed intervals (burnTick)
            while (acc >= burnTick)
            {
                acc -= burnTick;
                float dmgThisTick = burnDps * burnTick; // float; rounded per-target below

                if (circle)
                {
                    // Prefer querying via the circle trigger for precise shape
                    var filter = new ContactFilter2D { useLayerMask = false, useTriggers = true };
                    if (enemyLayers.value != 0)
                    {
                        filter.useLayerMask = true;
                        filter.layerMask = enemyLayers;
                    }

                    var results = new List<Collider2D>(32);
                    int hitCount = circle.Overlap(filter, results);
                    if (hitCount > 0)
                    {
                        for (int i = 0; i < results.Count; i++)
                        {
                            var col = results[i];
                            if (!col) continue;

                            // Find EnemyHealth on the target (root or hitbox parent)
                            var eh = col.attachedRigidbody
                                ? col.attachedRigidbody.GetComponentInParent<EnemyHealth>()
                                : col.GetComponentInParent<EnemyHealth>();
                            if (!eh) continue;

                            int dmgInt = Mathf.RoundToInt(dmgThisTick);
                            if (dmgInt > 0)
                                eh.TakeDamage(dmgInt, DamageElement.Fire);
                        }
                    }
                }
                else
                {
                    // Fallback: overlap circle by world radius if no CircleCollider2D assigned
                    var hits = Physics2D.OverlapCircleAll(transform.position, radius,
                        enemyLayers.value != 0 ? enemyLayers : ~0);
                    for (int i = 0; i < hits.Length; i++)
                    {
                        var eh = hits[i].GetComponentInParent<EnemyHealth>();
                        if (!eh) continue;

                        int dmgInt = Mathf.RoundToInt(dmgThisTick);
                        if (dmgInt > 0)
                            eh.TakeDamage(dmgInt, DamageElement.Fire);
                    }
                }
            }

            yield return null;
        }

        Destroy(gameObject);
    }


    private void OnDestroy()
    {
        // clean up any slow sources (already cleared on Ignite, but safe)
        foreach (var kv in _oilExpiry)
            if (kv.Key) kv.Key.RemoveSource(this);
        _oilExpiry.Clear();
        _inside.Clear();

        for (int i = 0; i < _spawnedFireTiles.Count; i++)
            if (_spawnedFireTiles[i]) Destroy(_spawnedFireTiles[i]);
        _spawnedFireTiles.Clear();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = isBurning ? new Color(1f, 0.4f, 0.1f, 0.35f) : new Color(0.1f, 0.6f, 1f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }

    private void TryApplyFireDamage(GameObject target, float amount)
    {
        if (!target) return;

        // Works whether the collider is on the root or a child hitbox
        var eh = target.GetComponent<EnemyHealth>();
        if (eh == null) return;
        var bh = target.GetComponent<BossHealth>();
        if (bh == null) return;
        int dmg = Mathf.RoundToInt(amount);
        eh.TakeDamage(dmg, DamageElement.Fire);
        bh.TakeDamage(dmg, DamageElement.Fire);
    }
    private void SpawnFireTiles()
    {
        if (!fireTilePrefab) return;

        float R = radius;
        // target number ~ density × area
        int target = Mathf.Clamp(Mathf.CeilToInt(tilesPerUnit2 * Mathf.PI * R * R), 1, maxTiles);

        // Lay down a jittered grid over the bounding box, only keep points inside the circle
        // Cell size approximately the square root of area/target (blue-noise-ish)
        float cell = Mathf.Sqrt((Mathf.PI * R * R) / target);
        if (cell < 0.05f) cell = 0.05f; // don’t go crazy dense

        Vector2 center = transform.position;
        int cols = Mathf.CeilToInt((2f * R) / cell);
        int rows = cols;

        // Offset to center grid around the circle
        Vector2 origin = center - new Vector2(cols * cell, rows * cell) * 0.5f + new Vector2(cell * 0.5f, cell * 0.5f);

        int placed = 0;
        for (int y = 0; y < rows && placed < target; y++)
        {
            for (int x = 0; x < cols && placed < target; x++)
            {
                Vector2 p = origin + new Vector2(x * cell, y * cell);

                // keep only inside circle (with small margin)
                if ((p - center).sqrMagnitude > R * R) continue;

                // jitter for organic look
                Vector2 j = (jitter > 0f)
                    ? new Vector2(UnityEngine.Random.Range(-jitter, jitter), UnityEngine.Random.Range(-jitter, jitter))
                    : Vector2.zero;

                var go = Instantiate(fireTilePrefab, p + j, Quaternion.identity, transform);
                go.transform.localScale = Vector3.one * fireTileScale;
                _spawnedFireTiles.Add(go);
                var anim = go.GetComponent<Animator>();
                if (anim)
                {
                    // randomize animation phase so they don't all loop identically
                    var offset = UnityEngine.Random.Range(0f, 1f);
                    anim.Play(fireAnimationName, 0, offset);
                }
                placed++;
            }
        }
    }

    private EnemyHealth GetEnemyHealth(Collider2D other)
    {
        if (!other) return null;
        var go = other.attachedRigidbody ? other.attachedRigidbody.gameObject : other.gameObject;
        return go ? go.GetComponentInParent<EnemyHealth>() : null;
    }

}
