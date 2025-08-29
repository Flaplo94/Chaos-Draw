using UnityEngine;
using System.Collections;

public class LightningBall : MonoBehaviour, IAbilityBehavior
{
    [Header("Motion & Damage")]
    public float speed = 3f;
    public int damagePerTick = 1;
    public float tickInterval = 0.1f;
    public float duration = 5f;
    public float zapRadius = 1.5f;

    [Header("Optional VFX")]
    [SerializeField] private GameObject zapVisual;

    private Vector2 direction;
    private float tickTimer, lifeTimer;
    private Rigidbody2D rb;
    private Collider2D col;
    private Vector2 lastVelocity;

    public bool Initialize(Vector2 dir, Rarity rarity)
    {
        direction = dir.normalized;

        switch (rarity)
        {
            case Rarity.Uncommon: zapRadius *= 1.1f; duration += 1f; break;
            case Rarity.Rare: zapRadius *= 1.2f; duration += 1.5f; damagePerTick += 1; break;
            case Rarity.Epic: zapRadius *= 1.3f; duration += 2f; damagePerTick += 2; break;
            case Rarity.Legendary: zapRadius *= 1.5f; duration += 3f; damagePerTick += 3; break;
        }
        return true;
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        rb.gravityScale = 0f;
        rb.linearDamping = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && col != null)
        {
            var playerCols = player.GetComponentsInChildren<Collider2D>(true);
            for (int i = 0; i < playerCols.Length; i++)
                Physics2D.IgnoreCollision(col, playerCols[i], true);

            for (int i = 0; i < playerCols.Length; i++)
            {
                var dist = Physics2D.Distance(col, playerCols[i]);
                if (dist.isOverlapped)
                {
                    float nudge = 0.35f;
                    transform.position += (Vector3)(direction * nudge);
                    break;
                }
            }

            StartCoroutine(ReenablePlayerCollisionSoon(playerCols));
        }

        rb.linearVelocity = direction * speed;
        lastVelocity = rb.linearVelocity;
    }

    private IEnumerator ReenablePlayerCollisionSoon(Collider2D[] playerCols)
    {
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        for (int i = 0; i < playerCols.Length; i++)
        {
            if (playerCols[i] != null && col != null)
                Physics2D.IgnoreCollision(col, playerCols[i], false);
        }
    }

    private void FixedUpdate()
    {
        lastVelocity = rb.linearVelocity;
    }

    private void Update()
    {
        lifeTimer += Time.deltaTime;
        if (lifeTimer >= duration) { Destroy(gameObject); return; }

        tickTimer += Time.deltaTime;
        if (tickTimer >= tickInterval)
        {
            tickTimer = 0f;
            ZapNearby();
        }
    }

    private void ZapNearby()
    {
        var hits = Physics2D.OverlapCircleAll(transform.position, zapRadius);
        if (hits == null || hits.Length == 0) return;

        int finalTick = DamageCalculator.ComputeFinalDamage(damagePerTick, DamageElement.Lightning);

        foreach (var h in hits)
        {
            if (!h) continue;

            var root = h.attachedRigidbody ? h.attachedRigidbody.transform : h.transform;
            if (root.CompareTag("Player")) continue;
            if (root == transform || root.IsChildOf(transform)) continue;

            bool didDamage = false;
            if (h.TryGetComponent(out EnemyHealth eh)) { eh.TakeDamage(finalTick); didDamage = true; }
            if (h.TryGetComponent(out BossHealth bh)) { bh.TakeDamage(finalTick); didDamage = true; }
            if (!didDamage) continue;

            if (zapVisual != null)
            {
                var v = Instantiate(zapVisual);
                if (v.TryGetComponent<LineRenderer>(out var lr))
                {
                    lr.useWorldSpace = true;
                    if (lr.positionCount < 2) lr.positionCount = 2;
                    lr.SetPosition(0, transform.position);
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

    private void OnCollisionEnter2D(Collision2D c)
    {
        if (rb == null) return;

        Vector2 inVel = lastVelocity;
        if (inVel.sqrMagnitude < 0.0001f) inVel = rb.linearVelocity;

        Vector2 normal = c.contactCount > 0 ? c.GetContact(0).normal : -inVel.normalized;
        Vector2 reflected = Vector2.Reflect(inVel, normal);

        rb.linearVelocity = reflected.normalized * speed;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, zapRadius);
    }
#endif
}
