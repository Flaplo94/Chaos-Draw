using UnityEngine;

public class Fireball : MonoBehaviour, IAbilityBehavior
{
    [Header("Tuning")]
    public float speed = 10f;
    public int damage = 10;
    public float aoeRadius = 2f;

    private Vector2 direction;
    private bool impacted;
    private bool luckyWasDuplicated = false;

    private Animator anim;
    private Collider2D col;
    private Rigidbody2D rb;

    [Header("Audio")]
    [SerializeField] private AudioClip impactSound;
    [SerializeField] private AudioSource audioSource;
    [Header("Range")]
    public float range = 10f;
    [SerializeField] private bool explodeAtMaxRange = true;
    private Vector2 spawnPos;

    public bool Initialize(Vector2 dir, Rarity rarity)
    {
        direction = dir.normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        switch (rarity)
        {
            case Rarity.Uncommon: aoeRadius *= 1.2f; damage += 2; break;
            case Rarity.Rare: aoeRadius *= 1.4f; damage += 5; break;
            case Rarity.Epic: aoeRadius *= 1.6f; damage += 10; break;
            case Rarity.Legendary: aoeRadius *= 2f; damage += 20; break;
        }
        return true;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        col = GetComponent<Collider2D>();

        audioSource = GetComponent<AudioSource>() ?? audioSource;
        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
            if (impactSound != null) audioSource.clip = impactSound;
        }
    }

    void Start()
    {
        spawnPos = transform.position;
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.linearDamping = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.linearVelocity = direction * speed;
        }

        if (!luckyWasDuplicated)
        {
            LuckyShotSystem.OnSpellCast(() =>
            {
                Vector2 dir = (direction.sqrMagnitude > 0.0001f) ? direction.normalized : (Vector2)transform.right;
                Vector2 side = new Vector2(-dir.y, dir.x);
                Vector3 p2 = transform.position;

                if (LuckyShotSystem.TryConsumeSpawnOffset(out var off))
                {
                    float dist = (off.x != 0f) ? off.x : off.magnitude;
                    p2 += (Vector3)(side * dist);
                }

                var dup = Instantiate(gameObject, p2, transform.rotation);
                var comp = dup.GetComponent<Fireball>();
                if (comp != null)
                {
                    comp.luckyWasDuplicated = true;
                    comp.impacted = false;
                    comp.direction = dir;
                }
            });
        }
    }

    void Update()
    {
        if (impacted) return;

        if (range > 0f)
        {
            Vector2 d = (Vector2)transform.position - spawnPos;
            if (d.sqrMagnitude >= range * range)
            {
                if (explodeAtMaxRange) DoImpact();
                else Destroy(gameObject);
                return;
            }
        }

        if (rb == null)
            transform.position += (Vector3)direction * speed * Time.deltaTime;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (impacted) return;
        if (!other.CompareTag("Enemy") && !other.CompareTag("Boss")) return;

        DoImpact();
    }

    private void DoImpact()
    {
        if (impacted) return;
        impacted = true;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        speed = 0f;
        if (col) col.enabled = false;

        var hits = Physics2D.OverlapCircleAll(transform.position, aoeRadius);
        foreach (var h in hits)
        {
            var result = DamageCalculator.ComputeFinalDamage(damage, DamageElement.Fire);
            if (h.TryGetComponent(out EnemyHealth eh))
            {
                eh.TakeDamage(result.amount, result.element);
                BurnRules.TryApplyBurn(h.transform, result.amount);
            }
            if (h.TryGetComponent(out BossHealth bh))
            {
                bh.TakeDamage(result.amount, result.element);
                BurnRules.TryApplyBurn(h.transform, result.amount);
            }
        }

        float baseSpriteSize = 32f;
        float ppu = 32f;
        float worldSize = baseSpriteSize / ppu;
        float targetDiameter = aoeRadius * 2f;
        float scaleFactor = targetDiameter / worldSize;
        transform.localScale = new Vector3(scaleFactor, scaleFactor, 1f);

        if (anim) anim.SetTrigger("Impact");
    }

    public void OnImpactFinished() => Destroy(gameObject);

    public void PlayImpactSound()
    {
        if (audioSource != null && impactSound != null)
            audioSource.Play();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, aoeRadius);
    }
}
