using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlackHoleBehavior : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform visualRoot;            // assign the object with your animation and sprites
    [SerializeField] private CircleCollider2D damageCollider; // trigger on the damage core

    [Header("Visual Size")]
    [SerializeField, Min(0.1f)] private float size = 1f;      // slider to scale the visual
    private Vector3 initialVisualScale = Vector3.one;

    [Header("Match Settings")]
    [SerializeField, Range(0.1f, 1.0f)]
    private float damageToVisualRadius = 1.0f;                // 1.0 means damage radius equals visual radius
    [SerializeField] private bool suctionFollowsVisual = true;
    [SerializeField] private float suctionToVisual = 1.8f;    // suction radius = visualRadius * this

    [Header("Damage Over Time")]
    [SerializeField] private float damagePerTick = 2f;
    [SerializeField] private float tickInterval = 0.25f;
    [SerializeField] private LayerMask playerLayer;

    [Header("Lifetime")]
    [SerializeField] private float lifetime = 4f;

    // runtime state
    private float visualWorldRadius;
    private float worldDamageRadius;
    private float worldSuctionRadius;

    // ticking
    private readonly Dictionary<Collider2D, float> nextTickAt = new Dictionary<Collider2D, float>();
    private ContactFilter2D filter;
    private readonly List<Collider2D> overlaps = new List<Collider2D>(4);
    private static readonly List<Collider2D> tmpKeys = new List<Collider2D>();

    private void Awake()
    {
        if (visualRoot == null) visualRoot = transform;
        initialVisualScale = visualRoot.localScale;

        if (damageCollider == null) damageCollider = GetComponent<CircleCollider2D>();
        if (damageCollider != null) damageCollider.isTrigger = true;

        filter = new ContactFilter2D { useLayerMask = true, layerMask = playerLayer, useTriggers = true };

        ApplySizeVisualOnly();
        SyncColliderAndSuctionFromVisual();
    }

    private void OnEnable()
    {
        ApplySizeVisualOnly();
        SyncColliderAndSuctionFromVisual();
        StartCoroutine(DieAfter());
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (visualRoot == null) visualRoot = transform;
        if (Application.isPlaying == false)
        {
            // in edit mode, reflect slider changes immediately
            ApplySizeVisualOnly();
            SyncColliderAndSuctionFromVisual();
        }
    }
#endif

    private void LateUpdate()
    {
        // do not touch visual scale here
        // only keep collider and suction perfectly matched to the current visual bounds
        SyncColliderAndSuctionFromVisual();
    }

    private void ApplySizeVisualOnly()
    {
        if (visualRoot != null)
            visualRoot.localScale = initialVisualScale * size;
    }

    private void SyncColliderAndSuctionFromVisual()
    {
        visualWorldRadius = ComputeVisualWorldRadius(visualRoot);
        worldDamageRadius = Mathf.Max(0.01f, visualWorldRadius * damageToVisualRadius);
        if (suctionFollowsVisual)
            worldSuctionRadius = Mathf.Max(worldDamageRadius, visualWorldRadius * suctionToVisual);

        if (damageCollider != null)
        {
            float scaleFactor = GetMaxAxisLossyScale(damageCollider.transform); // convert world to local
            float localRadius = worldDamageRadius / Mathf.Max(0.0001f, scaleFactor);
            damageCollider.radius = localRadius;
        }
    }

    private static float ComputeVisualWorldRadius(Transform root)
    {
        if (root == null) return 0.5f;

        var renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
        if (renderers.Length == 0)
        {
            Vector3 s = root.lossyScale;
            return Mathf.Max(0.05f, Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y)) * 0.5f);
        }

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
        return Mathf.Max(b.extents.x, b.extents.y);
    }

    private static float GetMaxAxisLossyScale(Transform t)
    {
        Vector3 s = t.lossyScale;
        return Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y));
    }

    private IEnumerator DieAfter()
    {
        yield return new WaitForSeconds(lifetime);
        Destroy(gameObject);
    }

    private void FixedUpdate()
    {
        // suction uses world radius
        if (worldSuctionRadius > 0f)
        {
            Collider2D pullHit = Physics2D.OverlapCircle(transform.position, worldSuctionRadius, playerLayer);
            if (pullHit != null)
            {
                var rb = pullHit.attachedRigidbody;
                if (rb != null)
                {
                    Vector2 dir = ((Vector2)transform.position - rb.position).normalized;
                    rb.AddForce(dir * 18f, ForceMode2D.Force);
                }
            }
        }

        if (damageCollider == null) return;

        overlaps.Clear();
        damageCollider.Overlap(filter, overlaps); // Unity 6 API

        float now = Time.time;
        for (int i = 0; i < overlaps.Count; i++)
        {
            var col = overlaps[i];
            if (!nextTickAt.TryGetValue(col, out float due) || now >= due)
            {
                var health = col.GetComponentInParent<PlayerHealth>();
                if (health != null) health.TakeDamage(Mathf.RoundToInt(damagePerTick));
                nextTickAt[col] = now + tickInterval;
            }
        }

        if (nextTickAt.Count > 0)
        {
            tmpKeys.Clear();
            foreach (var kv in nextTickAt) tmpKeys.Add(kv.Key);
            for (int k = 0; k < tmpKeys.Count; k++)
                if (!overlaps.Contains(tmpKeys[k])) nextTickAt.Remove(tmpKeys[k]);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        float visR = ComputeVisualWorldRadius(visualRoot == null ? transform : visualRoot) * Mathf.Max(0.1f, size);
        float dmgR = Mathf.Max(0.01f, visR * Mathf.Clamp(damageToVisualRadius, 0.1f, 1.0f));
        float sucR = suctionFollowsVisual ? Mathf.Max(dmgR, visR * suctionToVisual) : visR;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, dmgR);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, sucR);
    }
#endif
}
