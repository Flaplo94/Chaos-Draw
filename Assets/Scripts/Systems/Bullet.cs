using UnityEngine;

public class Bullet : MonoBehaviour
{
    public int baseDamage = 1;
    public float lifetime = 5f;

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

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var enemy = other.GetComponent<EnemyHealth>()
                 ?? other.GetComponentInParent<EnemyHealth>()
                 ?? other.GetComponentInChildren<EnemyHealth>();

        if (enemy != null)
        {
            enemy.TakeDamage(baseDamage, DamageElement.Physical);
            LogDamage("Enemy", enemy.gameObject.name);
            Destroy(gameObject);
            return;
        }

        var boss = other.GetComponent<BossHealth>()
                ?? other.GetComponentInParent<BossHealth>()
                ?? other.GetComponentInChildren<BossHealth>();

        if (boss != null)
        {
            boss.TakeDamage(baseDamage); // BossHealth skal evt. ogs� have element
            LogDamage("Boss", boss.gameObject.name);
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
