using UnityEngine;

public class Bullet : MonoBehaviour
{
    public int baseDamage = 1;
    public float lifetime = 5f;

    [Header("Mana")]
    [SerializeField] private float manaOnHit = 5f;

    [Header("Audio")]
    [SerializeField] private AudioClip hitSfx;
    [SerializeField] private AudioSource audioSource; // assign in Inspector

    // --- Backwards compatibility ---
    public int damage
    {
        get => baseDamage;
        set => baseDamage = value;
    }

    [Header("Debug")]
    public bool logDamage = false;
    [HideInInspector] public int debugBaseDamage = 0;
    [HideInInspector] public float debugGlobalMult = 1f;
    [HideInInspector] public float debugElementMult = 1f;

    private Rigidbody2D rb;
    private Collider2D col;
    private SpriteRenderer sr;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        if (rb && rb.linearVelocity.sqrMagnitude > 0.01f)
        {
            float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle - 90f, Vector3.forward);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var enemy = other.GetComponent<EnemyHealth>()
                 ?? other.GetComponentInParent<EnemyHealth>()
                 ?? other.GetComponentInChildren<EnemyHealth>();

        if (enemy != null)
        {
            var result = DamageCalculator.ComputeFinalDamage(baseDamage, DamageElement.Physical);
            enemy.TakeDamage(result.amount, result.element);

            // Yeet Cube: count basic-attack hit (enemy)
            YeetCubeSystem.ReportPlayerHit(enemy.transform.root, isBoss: false);

            Hit("Enemy", enemy.gameObject.name, result);
            return;
        }

        var boss = other.GetComponent<BossHealth>()
                ?? other.GetComponentInParent<BossHealth>()
                ?? other.GetComponentInChildren<BossHealth>();

        if (boss != null)
        {
            var result = DamageCalculator.ComputeFinalDamage(baseDamage, DamageElement.Physical);
            boss.TakeDamage(result.amount, result.element);

            // Yeet Cube: count basic-attack hit (boss)
            YeetCubeSystem.ReportPlayerHit(boss.transform.root, isBoss: true);

            Hit("Boss", boss.gameObject.name, result);
            return;
        }

        if (logDamage)
        {
            string layerName = LayerMask.LayerToName(other.gameObject.layer);
            Debug.Log("[DMG?] Hit '" + other.gameObject.name + "' (layer=" + layerName + ") but no EnemyHealth/BossHealth found.");
        }
    }

    private void Hit(string targetType, string targetName, DamageResult result)
    {
        PlayHitSound();
        LogDamage(targetType, targetName, result);

        PlayerMana.Instance?.GainMana(manaOnHit);

        // Disable visuals + collider immediately
        if (col) col.enabled = false;
        if (sr) sr.enabled = false;
        if (rb) rb.linearVelocity = Vector2.zero;

        // Destroy after sound finishes
        Destroy(gameObject, hitSfx != null ? hitSfx.length : 0f);
    }

    void PlayHitSound()
    {
        if (hitSfx == null || audioSource == null) return;
        audioSource.PlayOneShot(hitSfx);
    }

    void LogDamage(string targetType, string targetName, DamageResult result)
    {
        if (!logDamage) return;
        Debug.Log(
            $"[DMG] {targetType} '{targetName}' <- {result.amount} " +
            $"(base {baseDamage}, mult={result.amount / (float)baseDamage:0.##}, element={result.element})"
        );
    }
}
