using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerThrowing : MonoBehaviour
{
    [Header("Projectile")]
    public GameObject cardPrefab;
    public Transform firePoint;
    public float cardSpeed = 10f;

    [Header("Fire")]
    public float baseCooldown = 0.30f;
    [SerializeField] private float spreadDegrees = 0f; // angle step per extra projectile

    [Header("Targeting")]
    [Tooltip("If true, the basic attack ignores player input and auto-fires at the closest enemy in range.")]
    public bool autoFireOnly = true;
    [Tooltip("How far to search for enemies when auto-aiming/auto-firing.")]
    public float targetSearchRadius = 30f;
    [Tooltip("Optional: set to your Enemies layer for faster queries.")]
    public LayerMask enemyLayer = 0;

    [Header("Refs")]
    public PlayerStats stats;

    [Header("Audio")]
    [SerializeField] private AudioClip attackSfx;
    [SerializeField] private AudioSource audioSource;

    [Header("Input System (kept, but not used when autoFireOnly = true)")]
    [SerializeField] private InputActionAsset inputActions;

    // Runtime
    [HideInInspector] public int extraProjectiles = 0;
    [HideInInspector] public float fireRateMult = 1f;
    [HideInInspector] public float projectileSpeedMult = 1f;

    private float cooldown;
    [SerializeField] private AttackCooldownIndicator cooldownIndicator;

    private Dimmer dimmer;
    private InputAction attackAction; // kept for compatibility

    void Awake()
    {
        dimmer = FindFirstObjectByType<Dimmer>();
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    void OnEnable()
    {
        if (inputActions != null)
        {
            var playerMap = inputActions.FindActionMap("Player");
            attackAction = playerMap != null ? playerMap.FindAction("Attack") : null;
            if (attackAction != null)
                attackAction.Enable(); // enabled but unused in autoFireOnly mode
        }

        var pbm = PlayerBuffManager.Instance;
        if (pbm != null)
        {
            pbm.OnValuesChanged += RecomputeFromBuffs;
            RecomputeFromBuffs();
        }
    }

    void OnDisable()
    {
        if (attackAction != null)
            attackAction.Disable();

        var pbm = PlayerBuffManager.Instance;
        if (pbm != null)
            pbm.OnValuesChanged -= RecomputeFromBuffs;
    }

    void Update()
    {
        if (cooldown > 0f) cooldown -= Time.unscaledDeltaTime;
        if (cooldown < 0f) cooldown = 0f;

        if (autoFireOnly)
        {
            TryAutoAttack();   // NEW: always-on auto fire loop
        }
        else
        {
            // Legacy/manual mode (kept, but not used when autoFireOnly=true)
            if (attackAction != null && attackAction.IsPressed())
                TryManualFire();
        }
    }

    // ====== AUTO FIRE LOOP ======
    private void TryAutoAttack()
    {
        if (cooldown > 0f) return;
        if (dimmer != null && dimmer.dimmerOn) return;
        if (!firePoint || !cardPrefab) return;

        if (TryGetNearestEnemyDirection(out var aimDir))
        {
            ThrowTowardDirection(aimDir);

            if (attackSfx != null && audioSource != null)
                audioSource.PlayOneShot(attackSfx);

            float effective = baseCooldown / Mathf.Max(0.01f, fireRateMult);
            cooldown = effective;

            if (cooldownIndicator != null)
                cooldownIndicator.StartCooldown(effective, transform);
        }
        // else: no enemy in range do nothing (no firing)
    }

    // Kept for compatibility (manual mode)
    private void TryManualFire()
    {
        if (cooldown > 0f) return;
        if (dimmer != null && dimmer.dimmerOn) return;

        // Prefer auto-aim if an enemy is in range even in manual mode
        if (TryGetNearestEnemyDirection(out var aimDir))
        {
            ThrowTowardDirection(aimDir);
        }
        else
        {
            ThrowTowardMouse();
        }

        if (attackSfx != null && audioSource != null)
            audioSource.PlayOneShot(attackSfx);

        float effective = baseCooldown / Mathf.Max(0.01f, fireRateMult);
        cooldown = effective;

        if (cooldownIndicator != null)
            cooldownIndicator.StartCooldown(effective, transform);
    }

    // ======= Targeting helpers =======
    private bool TryGetNearestEnemyDirection(out Vector2 dir)
    {
        dir = Vector2.zero;
        if (!firePoint) return false;

        Vector3 origin = firePoint.position;
        float bestDistSqr = float.PositiveInfinity;
        Transform bestTarget = null;

        Collider2D[] hits = (enemyLayer.value != 0)
            ? Physics2D.OverlapCircleAll(origin, targetSearchRadius, enemyLayer)
            : Physics2D.OverlapCircleAll(origin, targetSearchRadius);

        foreach (var col in hits)
        {
            if (col == null) continue;
            Transform t = col.attachedRigidbody ? col.attachedRigidbody.transform : col.transform;
            if (!t || t == transform) continue;

            float d2 = (t.position - origin).sqrMagnitude;
            if (d2 < bestDistSqr)
            {
                bestDistSqr = d2;
                bestTarget = t;
            }
        }

        // Optional fallback via tag if needed
        if (bestTarget == null)
        {
            var all = GameObject.FindGameObjectsWithTag("Enemy");
            foreach (var go in all)
            {
                if (!go) continue;
                Vector3 to = go.transform.position - origin;
                if (to.sqrMagnitude > targetSearchRadius * targetSearchRadius) continue;

                float d2 = to.sqrMagnitude;
                if (d2 < bestDistSqr)
                {
                    bestDistSqr = d2;
                    bestTarget = go.transform;
                }
            }
        }

        if (bestTarget == null) return false;

        dir = ((Vector2)(bestTarget.position - firePoint.position)).normalized;
        return dir.sqrMagnitude > 0.0001f;
    }

    // ======= Firing implementations =======
    private void ThrowTowardMouse()
    {
        if (!cardPrefab || !firePoint) return;

        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(
            new Vector3(mouseScreen.x, mouseScreen.y, -Camera.main.transform.position.z)
        );
        mouseWorld.z = 0f;

        Vector2 dir = ((Vector2)(mouseWorld - firePoint.position)).normalized;
        ThrowTowardDirection(dir);
    }

    private void ThrowTowardDirection(Vector2 dir)
    {
        if (!cardPrefab || !firePoint) return;

        // Main
        SpawnCard(firePoint.position, dir);

        // Extras with alternating spread
        int extras = Mathf.Max(0, extraProjectiles);
        for (int k = 1; k <= extras; k++)
        {
            int side = (k % 2 == 1) ? -1 : 1;
            int step = (k + 1) / 2;
            float angle = side * step * spreadDegrees;

            Vector2 d = (Quaternion.Euler(0, 0, angle) * dir).normalized;
            SpawnCard(firePoint.position, d);
        }
    }

    private void SpawnCard(Vector3 pos, Vector2 dir)
    {
        var go = Instantiate(cardPrefab, pos, Quaternion.identity);

        if (go.TryGetComponent<Bullet>(out var bullet))
        {
            int baseDmg = bullet.damage;
            float globalMult = (stats != null) ? stats.damageMult : 1f;
            if (PlayerBuffManager.Instance != null)
                globalMult = PlayerBuffManager.Instance.GetGenericDamageMult();
            else if (stats != null)
                globalMult = stats.damageMult;

            bullet.debugBaseDamage = baseDmg;
            bullet.debugGlobalMult = globalMult;
            bullet.debugElementMult = 1f;

            bullet.damage = Mathf.Max(1, Mathf.RoundToInt(baseDmg * globalMult));
        }

        if (go.TryGetComponent<Rigidbody2D>(out var rb))
            rb.linearVelocity = dir * (cardSpeed * Mathf.Max(0.01f, projectileSpeedMult));
    }

    private void RecomputeFromBuffs()
    {
        var pbm = PlayerBuffManager.Instance;
        if (pbm == null)
        {
            extraProjectiles = 0;
            fireRateMult = 1f;
            return;
        }

        extraProjectiles = Mathf.Max(0, pbm.GetExtraProjectiles());
        fireRateMult = Mathf.Max(0.01f, pbm.GetAttackSpeedMult());
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!firePoint) return;
        Gizmos.color = new Color(1f, 1f, 1f, 0.25f);
        Gizmos.DrawWireSphere(firePoint.position, targetSearchRadius);
    }
#endif
}
