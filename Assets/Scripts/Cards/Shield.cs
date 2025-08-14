using UnityEngine;

public class Shield : MonoBehaviour, IAbilityBehavior
{
    public static Shield Active; // current active shield (used by PlayerHealth)

    [Header("Base")]
    [SerializeField] private int baseShieldHealth = 1;   // hits the shield can take at Common
    [SerializeField] private float duration = 5f;        // 0 = endless until hits are consumed

    [Header("Reflect (Rare+)")]
    [SerializeField] private float reflectChance = 0f;   // 0..1
    [SerializeField] private int reflectDamage = 1;
    [SerializeField] private float reflectRadius = 1.75f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Visual")]
    [SerializeField] private Color activeTint = Color.blue;

    private int currentHealth;
    private Transform player;
    private SpriteRenderer playerSR;
    private Color originalColor;

    // Called by Ability right after Instantiate
    public bool Initialize(Vector2 _, Rarity rarity)
    {
        // Replace any existing shield
        if (Active != null && Active != this)
        {
            UIMessage uiMessage = FindFirstObjectByType<UIMessage>();
            if (uiMessage != null)
            {
                uiMessage.ShowMessage("Shield is already active");
            }
            Destroy(gameObject);
            return false; // already active, do not consume card
        }
        Active = this;
        currentHealth = baseShieldHealth;

        // Rarity scaling: more hits + reflect chance from Rare+
        switch (rarity)
        {
            case Rarity.Uncommon:
                currentHealth += 1;
                break;
            case Rarity.Rare:
                currentHealth += 2;
                reflectChance = 0.20f;
                break;
            case Rarity.Epic:
                currentHealth += 3;
                reflectChance = 0.35f;
                reflectDamage = Mathf.Max(reflectDamage, 2);
                break;
            case Rarity.Legendary:
                currentHealth += 5;
                reflectChance = 0.50f;
                reflectDamage = Mathf.Max(reflectDamage, 3);
                break;
                // Common: baseline
        }

        if (duration < 0f) duration = 0f;
        return true;
    }

    private void Start()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            Debug.LogWarning("Shield: No Player found. Destroying shield.");
            DestroySelf();
            return;
        }

        player = playerObj.transform;
        playerSR = player.GetComponent<SpriteRenderer>();

        // Tint player while shield is active (like before)
        if (playerSR != null)
        {
            originalColor = playerSR.color;
            playerSR.color = activeTint;
        }

        if (duration > 0f)
            Invoke(nameof(DestroySelf), duration);

        // Make sure this object is invisible & harmless in the scene
        // (no visuals/collisions needed for this controller)
        HideOwnRenderersAndColliders();
    }

    private void HideOwnRenderersAndColliders()
    {
        foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
        foreach (var c in GetComponentsInChildren<Collider2D>()) c.enabled = false;
    }

    /// <summary>
    /// Called by PlayerHealth before applying damage to HP.
    /// Returns true if the hit was consumed by the shield.
    /// </summary>
    public bool ConsumeHit()
    {
        // Optional: reflect on block (Rare+)
        if (reflectChance > 0f && Random.value < reflectChance)
            ReflectNearestEnemy();

        currentHealth--;
        if (currentHealth <= 0)
        {
            DestroySelf();
        }
        return true; // hit consumed
    }

    private void ReflectNearestEnemy()
    {
        if (player == null) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(player.position, reflectRadius, enemyLayer);
        if (hits == null || hits.Length == 0) return;

        // Pick the closest enemy/boss
        Collider2D closest = null;
        float best = float.MaxValue;
        foreach (var h in hits)
        {
            if (h == null) continue;
            float d = Vector2.Distance(player.position, h.transform.position);
            if (d < best)
            {
                best = d;
                closest = h;
            }
        }

        if (closest == null) return;

        if (closest.TryGetComponent(out EnemyHealth eh))
            eh.TakeDamage(reflectDamage);
        else if (closest.TryGetComponent(out BossHealth bh))
            bh.TakeDamage(reflectDamage);
    }

    private void OnDestroy()
    {
        // Restore player tint when shield ends
        if (playerSR != null)
            playerSR.color = originalColor;

        if (Active == this)
            Active = null;
    }

    private void DestroySelf()
    {
        // Cancel any pending invoke to be safe
        CancelInvoke();
        Destroy(gameObject);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Visualize reflect radius in editor
        Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.5f);
        if (player != null) Gizmos.DrawWireSphere(player.position, reflectRadius);
    }
#endif
}
