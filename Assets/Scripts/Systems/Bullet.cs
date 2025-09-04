using UnityEngine;

public class Bullet : MonoBehaviour
{
    public int baseDamage = 1;
    public float lifetime = 5f;

    [Header("Mana")]
    [SerializeField] private float manaOnHit = 5f;   // mana gain per hit

    [Header("Audio")]
    [SerializeField] private AudioClip hitSfx;   // assign in Inspector
    [SerializeField] private float hitVolume = 1f;

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

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
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
            PlayHitSound();
            LogDamage("Enemy", enemy.gameObject.name, result);

            PlayerMana.Instance?.GainMana(manaOnHit); // mana refund on hit

            Destroy(gameObject);
            return;
        }

        var boss = other.GetComponent<BossHealth>()
                ?? other.GetComponentInParent<BossHealth>()
                ?? other.GetComponentInChildren<BossHealth>();

        if (boss != null)
        {
            var result = DamageCalculator.ComputeFinalDamage(baseDamage, DamageElement.Physical);
            boss.TakeDamage(result.amount, result.element);
            PlayHitSound();
            LogDamage("Boss", boss.gameObject.name, result);

            PlayerMana.Instance?.GainMana(manaOnHit); // mana refund on hit

            Destroy(gameObject);
            return;
        }

        if (logDamage)
        {
            string layerName = LayerMask.LayerToName(other.gameObject.layer);
            Debug.Log("[DMG?] Hit '" + other.gameObject.name + "' (layer=" + layerName + ") but no EnemyHealth/BossHealth found.");
        }
    }

    void PlayHitSound()
    {
        if (hitSfx == null) return;

        GameObject temp = new GameObject("BulletHitSound");
        temp.transform.position = transform.position;

        AudioSource src = temp.AddComponent<AudioSource>();
        src.clip = hitSfx;
        src.volume = hitVolume;
        src.spatialBlend = 0f; // 2D sound
        src.Play();

        Destroy(temp, hitSfx.length);
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
