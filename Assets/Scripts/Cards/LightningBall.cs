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
    [SerializeField] private LayerMask enemyLayer;

    [Header("Lightning Visual (Animator + SpriteRenderer)")]
    [SerializeField] private GameObject lightningVisual;
    [SerializeField] private string playTrigger = "Play";
    [SerializeField] private string stateName = "Zap";
    [SerializeField] private float widthScale = 1f;
    [SerializeField] private float animSpeed = 1.0f;

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
            case Rarity.Rare: zapRadius *= 1.2f; duration += 1.5f; damagePerTick++; break;
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

    private void FixedUpdate() => lastVelocity = rb.linearVelocity;

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
        Collider2D[] hits = (enemyLayer.value == 0)
            ? Physics2D.OverlapCircleAll(transform.position, zapRadius)
            : Physics2D.OverlapCircleAll(transform.position, zapRadius, enemyLayer);

        if (hits == null || hits.Length == 0) return;

        DamageResult result = DamageCalculator.ComputeFinalDamage(damagePerTick, DamageElement.Lightning);

        foreach (var h in hits)
        {
            if (!h) continue;
            Transform root = h.attachedRigidbody ? h.attachedRigidbody.transform : h.transform;
            if (root.CompareTag("Player")) continue;

            if (h.TryGetComponent(out EnemyHealth eh)) eh.TakeDamage(result.amount, result.element);
            if (h.TryGetComponent(out BossHealth bh)) bh.TakeDamage(result.amount, result.element);

            if (lightningVisual != null) SpawnBolt(transform.position, root.position);
        }
    }

    private void SpawnBolt(Vector3 from, Vector3 to)
    {
        var go = Instantiate(lightningVisual);
        go.name = "LightningBall_Bolt";

        Vector3 mid = (from + to) * 0.5f;
        go.transform.position = mid;

        Vector2 dir = (to - from);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        go.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        float length = dir.magnitude;
        var sr = go.GetComponentInChildren<SpriteRenderer>();
        if (sr && sr.sprite)
        {
            float currentWidth = sr.bounds.size.x;
            if (currentWidth <= 0f)
                currentWidth = sr.sprite.rect.width / sr.sprite.pixelsPerUnit * go.transform.lossyScale.x;

            if (currentWidth > 0f)
            {
                float mul = length / currentWidth;
                go.transform.localScale = new Vector3(go.transform.localScale.x * mul,
                                                       go.transform.localScale.y * widthScale,
                                                       go.transform.localScale.z);
            }
            else go.transform.localScale = new Vector3(length, widthScale, 1f);
        }
        else go.transform.localScale = new Vector3(length, widthScale, 1f);

        var anim = go.GetComponentInChildren<Animator>();
        float ttl = 0.2f;
        if (anim)
        {
            anim.speed = animSpeed;
            anim.Rebind();
            anim.Update(0f);
            if (!string.IsNullOrEmpty(playTrigger)) anim.SetTrigger(playTrigger);
            else if (!string.IsNullOrEmpty(stateName)) anim.Play(stateName, 0, 0f);
            var st = anim.GetCurrentAnimatorStateInfo(0);
            ttl = st.length > 0 ? st.length / anim.speed : ttl;
        }
        Destroy(go, ttl);
    }

    private void OnCollisionEnter2D(Collision2D c)
    {
        if (!rb) return;
        Vector2 reflected = Vector2.Reflect(lastVelocity, c.contacts[0].normal);
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
