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

    [Header("Refs")]
    public PlayerStats stats;

    [Header("Audio")]
    [SerializeField] private AudioClip attackSfx;
    [SerializeField] private AudioSource audioSource;

    [Header("Input System")]
    [SerializeField] private InputActionAsset inputActions;

    // Runtime (kept for compatibility with other scripts / inspector)
    [HideInInspector] public int extraProjectiles = 0;
    [HideInInspector] public float fireRateMult = 1f; // <— RESTORED
    [HideInInspector] public float projectileSpeedMult = 1f;

    private float cooldown;
    [SerializeField] private AttackCooldownIndicator cooldownIndicator;

    private Dimmer dimmer;
    private InputAction attackAction;

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
                attackAction.Enable();
        }

        // Subscribe to buff changes so fireRateMult/extraProjectiles stay in sync
        var pbm = PlayerBuffManager.Instance;
        if (pbm != null)
        {
            pbm.OnValuesChanged += RecomputeFromBuffs;
            RecomputeFromBuffs(); // initialize now
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

        // Hold-to-fire
        if (attackAction != null && attackAction.IsPressed())
            TryFire();
    }

    private void TryFire()
    {
        if (cooldown > 0f) return;
        if (dimmer != null && dimmer.dimmerOn) return;

        ThrowTowardMouse();

        if (attackSfx != null && audioSource != null)
            audioSource.PlayOneShot(attackSfx);

        // Cooldown scales with attack speed (1.10 = 10% faster  shorter CD)
        float effective = baseCooldown / Mathf.Max(0.01f, fireRateMult);
        cooldown = effective;

        if (cooldownIndicator != null)
            cooldownIndicator.StartCooldown(effective, transform);
    }

    void ThrowTowardMouse()
    {
        if (!cardPrefab || !firePoint) return;

        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(
            new Vector3(mouseScreen.x, mouseScreen.y, -Camera.main.transform.position.z)
        );
        mouseWorld.z = 0f;

        Vector2 dir = ((Vector2)(mouseWorld - firePoint.position)).normalized;

        // Main projectile
        SpawnCard(firePoint.position, dir);

        // Exactly N extras with alternating spread around aim
        int extras = Mathf.Max(0, extraProjectiles);
        for (int k = 1; k <= extras; k++)
        {
            int side = (k % 2 == 1) ? -1 : 1; // left, right, left, right...
            int step = (k + 1) / 2;           // 1,1,2,2,3,3...
            float angle = side * step * spreadDegrees;

            Vector2 d = (Quaternion.Euler(0, 0, angle) * dir).normalized;
            SpawnCard(firePoint.position, d);
        }
    }

    void SpawnCard(Vector3 pos, Vector2 dir)
    {
        var go = Instantiate(cardPrefab, pos, Quaternion.identity);

        if (go.TryGetComponent<Bullet>(out var bullet))
        {
            int baseDmg = bullet.damage;
            float globalMult = (stats != null) ? stats.damageMult : 1f;
            if (PlayerBuffManager.Instance != null)
                globalMult = PlayerBuffManager.Instance.GetGenericDamageMult(); // 1.0 at fresh run
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

    // Keep local fields synced with PlayerBuffManager
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
        fireRateMult = Mathf.Max(0.01f, pbm.GetAttackSpeedMult()); // Gloves of Speed, etc. :contentReference[oaicite:1]{index=1}
    }
}
