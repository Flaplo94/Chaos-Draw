using UnityEngine;
using System.Collections.Generic;

public class LightningRod : MonoBehaviour, IAbilityBehavior
{
    [Header("Tuning")]
    [SerializeField] private float lifetime = 8f;
    [SerializeField] private float rodRange = 5f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float damageTickRate = 0.5f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Visual")]
    [Tooltip("Bolt prefab root with Animator + SpriteRenderer (set to loop in clip).")]
    [SerializeField] private GameObject lightningVisual;
    [Tooltip("Thickness multiplier on Y after stretching the root along X.")]
    [SerializeField] private float boltWidthScale = 1f;

    private float damageTimer = 0f;
    private Transform player;

    private static readonly List<LightningRod> activeRods = new List<LightningRod>();
    private readonly Dictionary<ConnKey, GameObject> bolts = new Dictionary<ConnKey, GameObject>(32);
    private readonly HashSet<ConnKey> seenThisFrame = new HashSet<ConnKey>();

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
        seenThisFrame.Clear();

        if (player && Vector2.Distance(player.position, transform.position) <= rodRange)
            HandleConnection(transform, player);

        for (int i = 0; i < activeRods.Count; i++)
        {
            var other = activeRods[i];
            if (!other || other == this) continue;

            float dist = Vector2.Distance(other.transform.position, transform.position);
            if (dist <= rodRange)
                HandleConnection(transform, other.transform);
        }

        // Destroy any bolts whose connections ended
        var toRemove = new List<ConnKey>();
        foreach (var kv in bolts)
        {
            if (!seenThisFrame.Contains(kv.Key))
            {
                if (kv.Value) Destroy(kv.Value);
                toRemove.Add(kv.Key);
            }
        }
        for (int i = 0; i < toRemove.Count; i++)
            bolts.Remove(toRemove[i]);
    }

    private void HandleConnection(Transform a, Transform b)
    {
        var key = new ConnKey(a, b);
        seenThisFrame.Add(key);

        GameObject bolt;
        if (!bolts.TryGetValue(key, out bolt) || !bolt)
        {
            bolt = Instantiate(lightningVisual);
            bolt.name = "LightningRod_Bolt";

            var rt = bolt.AddComponent<BoltRuntime>();
            rt.sr = bolt.GetComponentInChildren<SpriteRenderer>();
            rt.baseScale = bolt.transform.localScale;
            rt.baseWidth = CalculateWorldWidth(rt.sr, bolt.transform.lossyScale.x);

            bolts[key] = bolt;
        }

        // Position, rotate, and scale the root so it spans A to B
        Vector3 from = a.position;
        Vector3 to = b.position;
        Vector3 mid = (from + to) * 0.5f;
        bolt.transform.position = mid;

        Vector2 dir = (to - from);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        bolt.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        float length = dir.magnitude;

        var rtBolt = bolt.GetComponent<BoltRuntime>();
        float baseWidth = (rtBolt != null && rtBolt.baseWidth > 0f) ? rtBolt.baseWidth : 1f;
        Vector3 baseScale = (rtBolt != null) ? rtBolt.baseScale : Vector3.one;
        float mul = (baseWidth > 0f) ? (length / baseWidth) : length;
        bolt.transform.localScale = new Vector3(baseScale.x * mul,
                                                baseScale.y * boltWidthScale,
                                                baseScale.z);

        // Damage along the connection on tick
        if (damageTimer <= 0f)
        {
            if (length > 0.001f)
            {
                Vector2 n = dir.normalized;
                RaycastHit2D[] hits = (enemyLayer.value == 0)
                    ? Physics2D.RaycastAll(from, n, length)
                    : Physics2D.RaycastAll(from, n, length, enemyLayer);

                for (int i = 0; i < hits.Length; i++)
                {
                    var col = hits[i].collider;
                    if (!col) continue;

                    if (col.TryGetComponent(out EnemyHealth eh)) eh.TakeDamage(Mathf.RoundToInt(damage));
                    if (col.TryGetComponent(out BossHealth bh)) bh.TakeDamage(Mathf.RoundToInt(damage));
                }
            }
            damageTimer = damageTickRate;
        }
    }

    private static float CalculateWorldWidth(SpriteRenderer sr, float lossyScaleX)
    {
        if (sr && sr.sprite)
        {
            float w = sr.bounds.size.x;
            if (w > 0f) return w;
            return (sr.sprite.rect.width / sr.sprite.pixelsPerUnit) * lossyScaleX;
        }
        return 1f;
    }

    private void OnDestroy()
    {
        foreach (var kv in bolts)
            if (kv.Value) Destroy(kv.Value);
        bolts.Clear();
        activeRods.Remove(this);
    }

    private void DestroySelf()
    {
        CancelInvoke();
        Destroy(gameObject);
    }

    // Connection key that is order independent
    private readonly struct ConnKey
    {
        private readonly int aId;
        private readonly int bId;

        public ConnKey(Transform a, Transform b)
        {
            int ia = a ? a.GetInstanceID() : 0;
            int ib = b ? b.GetInstanceID() : 0;
            if (ia <= ib) { aId = ia; bId = ib; }
            else { aId = ib; bId = ia; }
        }

        public override int GetHashCode() => (aId * 486187739) ^ bId;
        public override bool Equals(object obj)
        {
            if (obj is ConnKey k) return k.aId == aId && k.bId == bId;
            return false;
        }
    }

    // Per-bolt cached data
    private class BoltRuntime : MonoBehaviour
    {
        public SpriteRenderer sr;
        public Vector3 baseScale;
        public float baseWidth;
    }
}
