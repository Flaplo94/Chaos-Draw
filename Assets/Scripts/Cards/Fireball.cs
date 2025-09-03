using UnityEngine;

public class Fireball : MonoBehaviour, IAbilityBehavior
{
    public float speed = 10f;
    public int damage = 10;
    public float aoeRadius = 2f;

    private Vector2 direction;
    private bool impacted;

    private Animator anim;
    private Collider2D col;

    [Header("Audio")]
    [SerializeField] private AudioClip impactSound;
    [SerializeField] private AudioSource audioSource;

    public bool Initialize(Vector2 dir, Rarity rarity)
    {
        direction = dir.normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        switch (rarity)
        {
            case Rarity.Uncommon: aoeRadius *= 1.2f; break;
            case Rarity.Rare: aoeRadius *= 1.4f; damage += 5; break;
            case Rarity.Epic: aoeRadius *= 1.6f; damage += 10; break;
            case Rarity.Legendary: aoeRadius *= 2f; damage += 20; break;
        }
        return true;
    }

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
        audioSource.clip = impactSound;
        anim = GetComponent<Animator>();
        col = GetComponent<Collider2D>();
    }

    void Update()
    {
        if (impacted) return;
        transform.position += (Vector3)direction * speed * Time.deltaTime;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (impacted) return;
        if (!other.CompareTag("Enemy") && !other.CompareTag("Boss"))
            return;

        DoImpact();
    }

    private void DoImpact()
    {
        if (impacted) return;
        impacted = true;

        speed = 0f;
        if (col) col.enabled = false;

        var hits = Physics2D.OverlapCircleAll(transform.position, aoeRadius);
        foreach (var h in hits)
        {
            var result = DamageCalculator.ComputeFinalDamage(damage, DamageElement.Fire);
            if (h.TryGetComponent(out EnemyHealth eh)) eh.TakeDamage(result.amount, result.element);
            if (h.TryGetComponent(out BossHealth bh)) bh.TakeDamage(result.amount, result.element);
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
        if (audioSource != null && impactSound != null) audioSource.Play();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, aoeRadius);
    }
}
