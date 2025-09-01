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
    [SerializeField] private Animator shieldAnimator;
    [SerializeField] private string hitTriggerName = "Hit";
    [SerializeField] private SpriteRenderer shieldSR;

    [Header("Follow")]
    [SerializeField] private Transform followTarget;
    [SerializeField] private Vector3 followOffset = Vector3.zero;

    [Header("Sizing")]
    [SerializeField, Min(0f)] private float shieldRadius = 1f; // manual scale in inspector

    private int currentHealth;

    void Awake()
    {
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
        if (followTarget == null)
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) followTarget = playerObj.transform;
        }

        if (followTarget == null)
        {
            Debug.LogError("Shield: No followTarget found.");
            Destroy(gameObject);
            return;
        }

        transform.SetParent(followTarget, false);
        transform.localPosition = followOffset;
        transform.localRotation = Quaternion.identity;

        ApplyManualScale();

        if (shieldSR != null)
        {
            shieldSR.enabled = true;
            shieldSR.sortingOrder = 1000;
        }

        foreach (var c in GetComponentsInChildren<Collider2D>()) c.enabled = false;
    }

    void LateUpdate()
    {
        if (followTarget != null)
        {
            transform.position = followTarget.position + followOffset;
            transform.rotation = followTarget.rotation;

            // Keep radius applied
            ApplyManualScale();
        }
    }

    private void ApplyManualScale()
    {
        if (shieldSR == null || shieldSR.sprite == null) return;

        float baseDiameter = Mathf.Max(shieldSR.sprite.bounds.size.x, shieldSR.sprite.bounds.size.y);
        if (baseDiameter <= 0f) return;

        float scale = (shieldRadius * 2f) / baseDiameter; // radius diameter
        transform.localScale = new Vector3(scale, scale, 1f);
    }

    public bool ConsumeHit()
    {
        if (reflectChance > 0f && Random.value < reflectChance) ReflectNearestEnemy();
        if (!string.IsNullOrEmpty(hitTriggerName) && shieldAnimator != null) shieldAnimator.SetTrigger(hitTriggerName);
        currentHealth--;
        if (currentHealth <= 0) DestroySelf();
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
            if (!h) continue;
            float d = Vector2.Distance(followTarget.position, h.transform.position);
            if (d < best) { best = d; closest = h; }
        }

        if (closest == null) return;
        if (closest.TryGetComponent(out EnemyHealth eh)) eh.TakeDamage(reflectDamage, DamageElement.Physical);
        else if (closest.TryGetComponent(out BossHealth bh)) bh.TakeDamage(reflectDamage, DamageElement.Physical);
    }

    void OnDestroy()
    {
        if (Active == this) Active = null;
    }

    private void DestroySelf() => Destroy(gameObject);

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        var center = followTarget ? followTarget.position : transform.position;
        Gizmos.DrawWireSphere(center, shieldRadius); // show shield radius for clarity
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(center, reflectRadius); // show reflect radius separately
    }
#endif
}
