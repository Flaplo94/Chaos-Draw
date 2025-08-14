using UnityEngine;

public class GodSpeed : MonoBehaviour, IAbilityBehavior
{
    public static GodSpeed Active;

    [Header("Base")]
    [SerializeField] private float duration = 5f;
    [SerializeField] private float speedMultiplier = 1.5f;

    [Header("Zap")]
    [SerializeField] private float zapRadius = 1.0f;
    [SerializeField] private int zapDamagePerTick = 1;
    [SerializeField] private float zapTickInterval = 0.12f;

    [Header("Visual")]
    [SerializeField] private Color activeTint = new Color(0.6f, 0.9f, 1f, 1f);
    [SerializeField] private GameObject zapVisual;

    private Transform player;
    private PlayerMovement playerMove;
    private SpriteRenderer playerSR;
    private Color originalColor;
    private float originalMoveSpeed;
    private float zapTimer;

    // NOTE: now returns bool
    public bool Initialize(Vector2 _, Rarity rarity)
    {
        // Block activation if another GodSpeed is active
        if (Active != null && Active != this)
        {
            UIMessage uiMessage = FindFirstObjectByType<UIMessage>();
            if (uiMessage != null)
            {
                uiMessage.ShowMessage("God Speed is already active");
            }
            Destroy(gameObject);   // Cancel this spawn
            return false;          // Tell caller: do not consume card
        }

        Active = this;

        // Rarity scaling
        switch (rarity)
        {
            case Rarity.Uncommon: duration += 1f; speedMultiplier *= 1.10f; zapRadius *= 1.05f; break;
            case Rarity.Rare: duration += 2f; speedMultiplier *= 1.20f; zapRadius *= 1.10f; zapDamagePerTick += 1; break;
            case Rarity.Epic: duration += 3f; speedMultiplier *= 1.30f; zapRadius *= 1.15f; zapDamagePerTick += 2; break;
            case Rarity.Legendary: duration += 4f; speedMultiplier *= 1.40f; zapRadius *= 1.20f; zapDamagePerTick += 3; break;
        }
        if (duration < 0f) duration = 0f;
        return true; // proceed
    }

    private void Awake()
    {
        var po = GameObject.FindGameObjectWithTag("Player");
        if (po != null)
        {
            transform.SetPositionAndRotation(po.transform.position, Quaternion.identity);
            transform.SetParent(po.transform, worldPositionStays: false);
        }

        foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
        foreach (var c in GetComponentsInChildren<Collider2D>()) c.enabled = false;
    }

    private void Start()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (!playerObj) { DestroySelf(); return; }

        player = playerObj.transform;
        playerMove = playerObj.GetComponent<PlayerMovement>();
        playerSR = playerObj.GetComponent<SpriteRenderer>();
        if (!playerMove) { DestroySelf(); return; }

        originalMoveSpeed = playerMove.moveSpeed;
        playerMove.moveSpeed = originalMoveSpeed * Mathf.Max(1f, speedMultiplier);

        if (playerSR != null)
        {
            originalColor = playerSR.color;
            playerSR.color = activeTint;
        }

        if (duration > 0f) Invoke(nameof(DestroySelf), duration);
    }

    private void Update()
    {
        if (!player) { DestroySelf(); return; }

        zapTimer += Time.deltaTime;
        if (zapTimer >= zapTickInterval)
        {
            zapTimer = 0f;
            ZapNearby();
        }
    }

    private void ZapNearby()
    {
        var hits = Physics2D.OverlapCircleAll(player.position, zapRadius);
        if (hits == null || hits.Length == 0) return;

        for (int i = 0; i < hits.Length; i++)
        {
            var h = hits[i];
            if (!h) continue;

            Transform root = h.attachedRigidbody ? h.attachedRigidbody.transform : h.transform;
            if (root == player) continue;
            if (root == transform || root.IsChildOf(transform)) continue;

            bool didDamage = false;
            if (h.TryGetComponent(out EnemyHealth eh)) { eh.TakeDamage(zapDamagePerTick); didDamage = true; }
            if (h.TryGetComponent(out BossHealth bh)) { bh.TakeDamage(zapDamagePerTick); didDamage = true; }
            if (!didDamage) continue;

            if (zapVisual != null)
            {
                var v = Instantiate(zapVisual);
                if (v.TryGetComponent<LineRenderer>(out var lr))
                {
                    lr.useWorldSpace = true;
                    if (lr.positionCount < 2) lr.positionCount = 2;
                    lr.SetPosition(0, player.position);
                    lr.SetPosition(1, h.transform.position);
                    Destroy(v, 0.1f);
                }
                else
                {
                    v.transform.position = h.transform.position;
                    Destroy(v, 0.15f);
                }
            }
        }
    }

    private void OnDestroy()
    {
        if (playerSR != null) playerSR.color = originalColor;
        if (playerMove != null) playerMove.moveSpeed = originalMoveSpeed;
        if (Active == this) Active = null;
    }

    private void DestroySelf()
    {
        CancelInvoke();
        Destroy(gameObject);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (player != null)
        {
            Gizmos.color = new Color(0.6f, 0.9f, 1f, 0.35f);
            Gizmos.DrawWireSphere(player.position, zapRadius);
        }
    }
#endif
}
