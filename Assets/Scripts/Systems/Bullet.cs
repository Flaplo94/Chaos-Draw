using UnityEngine;

public class Bullet : MonoBehaviour
{
    public int baseDamage = 1;   // tidligere 'damage'
    public float lifetime = 5f;

    // --- Backwards compatibility ---
    public int damage
    {
        get => baseDamage;
        set => baseDamage = value;
    }

    // --- DEBUG ---
    [Header("Debug")]
    public bool logDamage = false;        // slå til på prefab når du vil se logs
    [HideInInspector] public int debugBaseDamage = 0;
    [HideInInspector] public float debugGlobalMult = 1f;
    [HideInInspector] public float debugElementMult = 1f;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Find EnemyHealth/BossHealth robust (child/parent)
        var enemy = other.GetComponent<EnemyHealth>()
                 ?? other.GetComponentInParent<EnemyHealth>()
                 ?? other.GetComponentInChildren<EnemyHealth>();

        if (enemy != null)
        {
            enemy.TakeDamage(baseDamage);
            LogDamage("Enemy", enemy.gameObject.name);
            Destroy(gameObject);
            return;
        }

        var boss = other.GetComponent<BossHealth>()
                ?? other.GetComponentInParent<BossHealth>()
                ?? other.GetComponentInChildren<BossHealth>();

        if (boss != null)
        {
            boss.TakeDamage(baseDamage);
            LogDamage("Boss", boss.gameObject.name);
            Destroy(gameObject);
            return;
        }

        // Debug-info hvis vi rammer noget uden health-script
        if (logDamage)
        {
            string layerName = LayerMask.LayerToName(other.gameObject.layer);
            Debug.Log("[DMG?] Hit '" + other.gameObject.name + "' (layer=" + layerName + ") but no EnemyHealth/BossHealth found.");
        }
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
