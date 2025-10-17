using System;
using UnityEngine;

[RequireComponent(typeof(HitFlash))]
public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private bool flashOnLethalHit = true;


    private int currentHealth;
    private HitFlash flash;
    private EnemyAnimator enemyAnimator;
    public Action OnDeath;
    private CircleCollider2D hitbox;

    [Header("Debug Gizmos")]
    [SerializeField] private bool showHitboxGizmos = true;
    [SerializeField] private bool includeChildrenColliders = true;

    // If true, only draw colliders on these layers (e.g. set to EnemyHurtbox layer)
    [SerializeField] private bool onlyHitboxLayer = false;
    [SerializeField] private LayerMask hitboxLayers;

    // Colors
    [SerializeField] private Color gizmoColor = new Color(0f, 1f, 0f, 0.85f);
    [SerializeField] private Color selectedGizmoColor = new Color(1f, 0.9f, 0.1f, 1f);



    // Globalt event (for alle enemies)
    public static event Action<EnemyHealth> OnAnyEnemyDied;

    private bool isDead = false;

    void Awake()
    {
        currentHealth = maxHealth;
        flash = GetComponent<HitFlash>() ?? GetComponent<HitFlash>();
        if (enemyAnimator == null)
            enemyAnimator = GetComponent<EnemyAnimator>();
        hitbox = GetComponent<CircleCollider2D>();
    }

    // === Standard damage entrypoint (bruges fortsat af alt eksisterende) ===
    public void TakeDamage(int amount, DamageElement element)
    {
        if (amount <= 0) return;
        if (isDead) return; // ignore damage after death

        currentHealth -= amount;

        // === Damage Numbers ===
        if (DamageNumbers.Instance != null)
            DamageNumbers.Instance.Show(transform.position, amount, element);

        // Lifesteal (centralized): heal player based on damage dealt
        DamageCalculator.ApplyLifesteal(amount);

        if (flashOnLethalHit || currentHealth > 0)
            flash?.PlayFlash();

        if (currentHealth <= 0)
            Die();
    }

    // === Basic attacks: now just forward to standard damage (lifesteal handled centrally) ===
    public void TakeBasicAttackDamage(int amount, DamageElement element)
    {
        TakeDamage(amount, element);
    }

    public void Heal(int amount)
    {
        if (amount <= 0) return;
        if (isDead) return; // don't heal corpses
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
    }

    public int GetHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;

    void Die()
    {
        if (isDead) return;
        isDead = true;

        GetComponent<EnemyFollow>()?.Kill();
        GetComponent<FlyingEnemy>()?.Kill();
        var drops = GetComponent<EnemyMetaDrops>();
        if (drops) drops.GrantRewards();
        // Disable ALL colliders so bullets no longer hit
        foreach (var col in GetComponentsInChildren<Collider2D>())
            col.enabled = false;

        OnAnyEnemyDied?.Invoke(this);

        enemyAnimator?.PlayDie();
    }

    // Called by animation event at the end of the death animation
    public void FinishDeath()
    {
        OnDeath?.Invoke();
        Destroy(gameObject);
    }
#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!showHitboxGizmos) return;
        DrawColliderGizmos(false);
    }

    void OnDrawGizmosSelected()
    {
        if (!showHitboxGizmos) return;
        DrawColliderGizmos(true);
    }

    void DrawColliderGizmos(bool selected)
    {
        var cols = includeChildrenColliders
            ? GetComponentsInChildren<Collider2D>(true)
            : GetComponents<Collider2D>();

        Gizmos.color = selected ? selectedGizmoColor : gizmoColor;

        foreach (var col in cols)
        {
            if (!col.enabled) continue;

            if (onlyHitboxLayer && hitboxLayers.value != 0)
            {
                int layer = col.gameObject.layer;
                if (((1 << layer) & hitboxLayers.value) == 0) continue;
            }

            switch (col)
            {
                case BoxCollider2D b:
                    DrawBox(b);
                    break;

                case CircleCollider2D c:
                    DrawCircle(c);
                    break;

                case PolygonCollider2D p:
                    DrawPolygon(p);
                    break;

                case CompositeCollider2D cc:
                    DrawComposite(cc);
                    break;

                case CapsuleCollider2D cap:
                    DrawBounds(cap.bounds);
                    break;

                default:
                    DrawBounds(col.bounds);
                    break;
            }
        }
    }

    // Helpers
    void DrawBox(BoxCollider2D b)
    {
        Gizmos.matrix = b.transform.localToWorldMatrix;
        Gizmos.DrawWireCube((Vector3)b.offset, (Vector3)b.size);
        Gizmos.matrix = Matrix4x4.identity;
    }

    void DrawCircle(CircleCollider2D c)
    {
        Gizmos.matrix = c.transform.localToWorldMatrix;
        Gizmos.DrawWireSphere((Vector3)c.offset, c.radius);
        Gizmos.matrix = Matrix4x4.identity;
    }

    void DrawPolygon(PolygonCollider2D p)
    {
        var t = p.transform;
        for (int path = 0; path < p.pathCount; path++)
        {
            var pts = p.GetPath(path);
            for (int i = 0; i < pts.Length; i++)
            {
                Vector3 a = t.TransformPoint(pts[i] + p.offset);
                Vector3 b = t.TransformPoint(pts[(i + 1) % pts.Length] + p.offset);
                Gizmos.DrawLine(a, b);
            }
        }
    }

    void DrawComposite(CompositeCollider2D c)
    {
        var t = c.transform;
        int pathCount = c.pathCount;
        for (int path = 0; path < pathCount; path++)
        {
            int pointCount = c.GetPathPointCount(path);
            var pts = new Vector2[pointCount];
            c.GetPath(path, pts);
            for (int i = 0; i < pointCount; i++)
            {
                Vector3 a = t.TransformPoint(pts[i]);
                Vector3 b = t.TransformPoint(pts[(i + 1) % pointCount]);
                Gizmos.DrawLine(a, b);
            }
        }
    }

    void DrawBounds(Bounds b)
    {
        Vector3 c = b.center;
        Vector3 s = b.size;
        Vector3 p1 = new Vector3(c.x - s.x / 2f, c.y - s.y / 2f, 0f);
        Vector3 p2 = new Vector3(c.x - s.x / 2f, c.y + s.y / 2f, 0f);
        Vector3 p3 = new Vector3(c.x + s.x / 2f, c.y + s.y / 2f, 0f);
        Vector3 p4 = new Vector3(c.x + s.x / 2f, c.y - s.y / 2f, 0f);
        Gizmos.DrawLine(p1, p2);
        Gizmos.DrawLine(p2, p3);
        Gizmos.DrawLine(p3, p4);
        Gizmos.DrawLine(p4, p1);
    }
#endif

}
