using UnityEngine;

public class Bullet : MonoBehaviour
{
    public int baseDamage = 1;
    public float lifetime = 5f;

    [Header("Audio")]
    [SerializeField] private AudioClip hitSfx;   // assign in Inspector
    [SerializeField] private float hitVolume = 1f;
    [SerializeField] private float ManaOnHit = 5f;
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
        var enemy = other.GetComponent<EnemyHealth>();

        if (enemy != null)
        {
            enemy.TakeDamage(baseDamage, DamageElement.Physical);
            PlayHitSound();
            LogDamage("Enemy", enemy.gameObject.name);

            // --- Mana Gain on enemy hit ---
            PlayerMana.Instance?.GainMana(ManaOnHit); // adjust amount per hit

            Destroy(gameObject);
            return;
        }

        var boss = other.GetComponent<BossHealth>()
                ?? other.GetComponentInParent<BossHealth>()
                ?? other.GetComponentInChildren<BossHealth>();

        if (boss != null)
        {
            boss.TakeDamage(baseDamage, DamageElement.Physical);
            PlayHitSound();
            LogDamage("Boss", boss.gameObject.name);

            // --- Mana Gain on boss hit ---
            PlayerMana.Instance?.GainMana(ManaOnHit); // adjust amount per hit

            Destroy(gameObject);
            return;
        }

        if (logDamage)
        {
            string layerName = LayerMask.LayerToName(other.gameObject.layer);
            
        }
    }

    void PlayHitSound()
    {
        if (hitSfx == null) return;

        // create temporary object to host AudioSource
        GameObject temp = new GameObject("BulletHitSound");
        temp.transform.position = transform.position;

        AudioSource src = temp.AddComponent<AudioSource>();
        src.clip = hitSfx;
        src.volume = hitVolume;
        src.spatialBlend = 0f; // 2D sound (instant, no distance delay)
        src.Play();

        // destroy temp object after sound is done
        Destroy(temp, hitSfx.length);
    }

    void LogDamage(string targetType, string targetName)
    {
        if (!logDamage) return;
        Debug.Log(
            "[DMG] " + targetType + " '" + targetName + "' <- " + baseDamage +
            " (base " + debugBaseDamage +
            ", global x" + debugGlobalMult.ToString("0.##") +
            ", elem x" + debugElementMult.ToString("0.##") + ")"
        );
    }
}
