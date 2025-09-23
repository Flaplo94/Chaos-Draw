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
    [SerializeField] private float radius = 10f;

    [Header("Visual")]
    [SerializeField] private GameObject lightningVisual;
    [SerializeField] private float boltWidthScale = 1f;

    [Header("Lucky Shot (perpendicular duplicate)")]
    [Tooltip("Sideways distance (world units) to place the duplicate 90° to the aim.")]
    [SerializeField] private float duplicateSideOffset = 1.5f;
    [Tooltip("If true, place the duplicate to the RIGHT of the aim; otherwise to the LEFT.")]
    [SerializeField] private bool offsetToRight = true;

    [Header("Stun")]
    [SerializeField] private bool applyStun = true;
    [SerializeField] private float stunDuration = 0.5f;

    private float damageTimer = 0f;
    private Transform player;
    private bool luckyWasDuplicated = false;        // prevents re-duplication
    private Vector2 castDir = Vector2.right;        // stored aim from Initialize

    private static readonly List<LightningRod> activeRods = new List<LightningRod>();
    private readonly Dictionary<ConnKey, GameObject> bolts = new Dictionary<ConnKey, GameObject>(32);
    private readonly HashSet<ConnKey> seenThisFrame = new HashSet<ConnKey>();

    public bool Initialize(Vector2 dir, Rarity rarity)
    {
        // store aim so we can place the duplicate perpendicular to it
        castDir = (dir.sqrMagnitude > 0.0001f) ? dir.normalized : Vector2.right;

        // your original rarity scaling
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

        // Lucky Shot: spawn a duplicate 90° to the aim, with inspector-controlled distance
        if (!luckyWasDuplicated)
        {
            LuckyShotSystem.OnSpellCast(this, () =>
            {
                // Build perpendicular (right/left) of the aim
                Vector2 dir = (castDir.sqrMagnitude > 0.0001f) ? castDir : Vector2.right;
                Vector2 rightOfAim = new Vector2(dir.y, -dir.x);
                Vector2 leftOfAim = new Vector2(-dir.y, dir.x);
                Vector2 side = offsetToRight ? rightOfAim : leftOfAim;

                Vector3 p2 = transform.position + (Vector3)(side.normalized * Mathf.Abs(duplicateSideOffset));

                var dup = Instantiate(gameObject, p2, transform.rotation);
                var comp = dup.GetComponent<LightningRod>();
                if (comp != null)
                {
                    comp.luckyWasDuplicated = true; // don’t chain
                    comp.castDir = this.castDir;    // keep same aim so “right side” is consistent
                }
            });
        }
    }

    private void Update()
    {
        damageTimer -= Time.deltaTime;
        seenThisFrame.Clear();

        // Connect to other rods
        for (int i = 0; i < activeRods.Count; i++)
        {
            var other = activeRods[i];
            if (!other || other == this) continue;

            float dist = Vector2.Distance(other.transform.position, transform.position);
            if (dist <= rodRange)
                HandleConnection(transform, other.transform);
        }

        // Connect to player
        if (player != null)
        {
            float distToPlayer = Vector2.Distance(player.position, transform.position);
            if (distToPlayer <= rodRange)
                HandleConnection(player, transform);
        }

        // Cleanup unused bolts
        var toRemove = new List<ConnKey>();
        foreach (var kv in bolts)
        {
            if (!seenThisFrame.Contains(kv.Key))
            {
                if (kv.Value) Destroy(kv.Value);
                toRemove.Add(kv.Key);
            }
        }
        foreach (var key in toRemove) bolts.Remove(key);
    }

    private void OnDestroy()
    {
        activeRods.Remove(this);

        foreach (var kv in bolts)
        {
            if (kv.Value) Destroy(kv.Value);
        }
        bolts.Clear();
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
            bolts[key] = bolt;
        }

        Vector3 from = a.position;
        Vector3 to = b.position;
        Vector3 mid = (from + to) * 0.5f;
        bolt.transform.position = mid;

        Vector2 dir = (to - from);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        bolt.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        float length = dir.magnitude;
        bolt.transform.localScale = new Vector3(length, boltWidthScale, 1f);

        if (damageTimer <= 0f)
        {
            var result = DamageCalculator.ComputeFinalDamage(damage, DamageElement.Lightning);

            RaycastHit2D[] hits = Physics2D.RaycastAll(from, dir.normalized, length, enemyLayer);
            for (int i = 0; i < hits.Length; i++)
            {
                var col = hits[i].collider;
                if (!col) continue;
                if (col.TryGetComponent(out EnemyHealth eh)) eh.TakeDamage(result.amount, result.element);
                if (col.TryGetComponent(out BossHealth bh)) bh.TakeDamage(result.amount, result.element);

                // NEW: stun
                if (applyStun)
                {
                    Transform root = col.attachedRigidbody ? col.attachedRigidbody.transform : col.transform;
                    StunReceiver.ApplyTo(root, stunDuration);
                }
            }
            damageTimer = damageTickRate;
        }
    }

    private void DestroySelf()
    {
        CancelInvoke();
        Destroy(gameObject);
    }

    private readonly struct ConnKey
    {
        private readonly int aId;
        private readonly int bId;

        public ConnKey(Transform a, Transform b)
        {
            aId = a ? a.GetInstanceID() : 0;
            bId = b ? b.GetInstanceID() : 0;
        }

        public override int GetHashCode() => (aId * 397) ^ bId;
        public override bool Equals(object obj)
        {
            if (obj is ConnKey other) return aId == other.aId && bId == other.bId;
            return false;
        }
    }
}
