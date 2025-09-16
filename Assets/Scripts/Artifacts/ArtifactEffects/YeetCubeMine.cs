using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class YeetCubeMine : MonoBehaviour
{
    [Header("Arming")]
    public float armDelay = 0.4f;
    public bool armed { get; private set; }

    [Header("Explosion")]
    public float radius = 1.8f;
    public int damage = 15;
    public DamageElement element = DamageElement.Fire;
    public LayerMask hitboxLayers;
    public bool requireHitboxMarker = false;
    public GameObject vfxPrefab; // optional pop FX

    float _t;

    void Awake()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true; // we’ll explode via overlap circle, not by trigger contact
    }

    void Update()
    {
        _t += Time.deltaTime;
        if (!armed && _t >= armDelay) armed = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!armed) return;
        // Optional: explode on contact with enemy hitbox
        if (hitboxLayers.value != 0 && ((1 << other.gameObject.layer) & hitboxLayers.value) == 0) return;
        if (requireHitboxMarker && other.GetComponent<EnemyHitbox>() == null) return;
        Explode();
    }

    public void Explode()
    {
        if (vfxPrefab) Instantiate(vfxPrefab, transform.position, Quaternion.identity);

        int mask = (hitboxLayers.value == 0) ? Physics2D.AllLayers : hitboxLayers.value;
        var hits = Physics2D.OverlapCircleAll(transform.position, radius, mask);
        var done = new System.Collections.Generic.HashSet<Transform>();

        foreach (var h in hits)
        {
            if (requireHitboxMarker && h.GetComponent<EnemyHitbox>() == null) continue;

            var root = h.transform.root;
            if (!done.Add(root)) continue;

            var result = DamageCalculator.ComputeFinalDamage(damage, element);

            if (root.TryGetComponent(out EnemyHealth eh))
                eh.TakeDamage(result.amount, result.element);

            if (root.TryGetComponent(out BossHealth bh))
                bh.TakeDamage(result.amount, result.element);

            // Report hit to YeetCube (helps Siphon Peck & Snowball)
            YeetCubeSystem.ReportPlayerHit(root, isBoss: bh != null);
        }

        Destroy(gameObject);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
#endif
}
