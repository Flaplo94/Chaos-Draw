using UnityEngine;

public class Shield : MonoBehaviour, IAbilityBehavior
{
    public static Shield Active;

    [Header("Base")]
    [SerializeField] private int baseShieldHealth = 1;

    [Header("Reflect (Rare+)")]
    [SerializeField] private float reflectChance = 0f;
    [SerializeField] private int reflectDamage = 1;
    [SerializeField] private float reflectRadius = 1.75f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Animation / Visuals")]
    [SerializeField] private Animator shieldAnimator;       // Animator on Visual child
    [SerializeField] private string hitTriggerName = "Hit"; // optional trigger in Animator
    [SerializeField] private SpriteRenderer shieldSR;       // SpriteRenderer on Visual child
    [SerializeField] private float fitPadding = 0.1f;       // world units padding around player

    [Header("Follow")]
    [SerializeField] private Transform followTarget;        // <-- assign the transform that ACTUALLY moves
    [SerializeField] private Vector3 followOffset = Vector3.zero;

    private int currentHealth;

    void Awake()
    {
        // Auto-find Visual components if not wired
        if (shieldAnimator == null) shieldAnimator = GetComponentInChildren<Animator>(true);
        if (shieldSR == null) shieldSR = GetComponentInChildren<SpriteRenderer>(true);
    }

    public bool Initialize(Vector2 _, Rarity rarity)
    {
        if (Active != null && Active != this)
        {
            var ui = FindFirstObjectByType<UIMessage>();
            if (ui) ui.ShowMessage("Shield is already active");
            Destroy(gameObject);
            return false;
        }

        Active = this;
        currentHealth = baseShieldHealth;

        switch (rarity)
        {
            case Rarity.Uncommon: currentHealth += 1; break;
            case Rarity.Rare: currentHealth += 2; reflectChance = 0.20f; break;
            case Rarity.Epic: currentHealth += 3; reflectChance = 0.35f; reflectDamage = Mathf.Max(reflectDamage, 2); break;
            case Rarity.Legendary: currentHealth += 5; reflectChance = 0.50f; reflectDamage = Mathf.Max(reflectDamage, 3); break;
        }
        return true;
    }

    void Start()
    {
        // Resolve follow target if not assigned
        if (followTarget == null)
        {
            // Prefer your actual moving component if you have one:
            // var pc = FindFirstObjectByType<PlayerController>();
            // if (pc) followTarget = pc.transform;
            // else fall back to the tagged Player:
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) followTarget = playerObj.transform;
        }

        if (followTarget == null)
        {
            Debug.LogError("Shield: No followTarget found. Assign it to the transform that actually moves.");
            Destroy(gameObject);
            return;
        }

        // Parent to the exact moving transform and reset locals
        transform.SetParent(followTarget, worldPositionStays: false);
        transform.localPosition = followOffset;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;

        // Auto-scale the Visual child to wrap the player's collider
        AutoScaleToFollowTarget();

        // Ensure renderers are visible & on top
        if (shieldSR != null)
        {
            shieldSR.enabled = true;
            shieldSR.sortingOrder = 1000;               // raise if needed
            // shieldSR.sortingLayerName = "Effects";    // if you use an Effects layer
        }

        // Disable any colliders on this prefab (shield should not block physics)
        foreach (var c in GetComponentsInChildren<Collider2D>()) c.enabled = false;

        // Sanity checks
        if (shieldAnimator != null && shieldAnimator.runtimeAnimatorController == null)
            Debug.LogError("Shield: Animator has no Controller assigned.");
        if (shieldSR == null) Debug.LogError("Shield: No SpriteRenderer found on Visual.");
    }

    void LateUpdate()
    {
        // Hard tether every frame (covers cases where parenting target is not the moving one)
        if (followTarget != null)
        {
            transform.position = followTarget.position + followOffset;
            transform.rotation = followTarget.rotation; // optional; remove if you don't want rotation
        }
    }

    private void AutoScaleToFollowTarget()
    {
        if (followTarget == null || shieldSR == null || shieldSR.sprite == null) return;

        // Use the collider that defines the player's size
        var pc = followTarget.GetComponentInChildren<Collider2D>();
        if (pc == null)
        {
            Debug.LogWarning("Shield: followTarget has no Collider2D in self/children; auto-scale skipped.");
            return;
        }

        // Desired diameter = max side of collider + padding
        float playerDiameter = Mathf.Max(pc.bounds.size.x, pc.bounds.size.y) + (fitPadding * 2f);

        // Base sprite diameter in world units (respects PPU)
        float baseDiameter = Mathf.Max(shieldSR.sprite.bounds.size.x, shieldSR.sprite.bounds.size.y);
        float scale = (baseDiameter > 0f) ? (playerDiameter / baseDiameter) : 1f;

        // Scale the Visual child (SpriteRenderer's transform), not the root
        shieldSR.transform.localScale = new Vector3(scale, scale, 1f);
        shieldSR.transform.localPosition = Vector3.zero;
    }

    public bool ConsumeHit()
    {
        if (reflectChance > 0f && Random.value < reflectChance)
            ReflectNearestEnemy();

        if (!string.IsNullOrEmpty(hitTriggerName) && shieldAnimator != null)
            shieldAnimator.SetTrigger(hitTriggerName);

        currentHealth--;
        if (currentHealth <= 0)
            DestroySelf();

        return true;
    }

    private void ReflectNearestEnemy()
    {
        if (followTarget == null) return;

        var hits = Physics2D.OverlapCircleAll(followTarget.position, reflectRadius, enemyLayer);
        if (hits == null || hits.Length == 0) return;

        Collider2D closest = null;
        float best = float.MaxValue;
        foreach (var h in hits)
        {
            if (h == null) continue;
            float d = Vector2.Distance(followTarget.position, h.transform.position);
            if (d < best) { best = d; closest = h; }
        }

        if (closest == null) return;

        if (closest.TryGetComponent(out EnemyHealth eh)) eh.TakeDamage(reflectDamage);
        else if (closest.TryGetComponent(out BossHealth bh)) bh.TakeDamage(reflectDamage);
    }

    void OnDestroy()
    {
        if (Active == this) Active = null;
    }

    private void DestroySelf()
    {
        Destroy(gameObject);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.5f);
        var center = followTarget ? followTarget.position : transform.position;
        Gizmos.DrawWireSphere(center, reflectRadius);
    }
#endif
}
