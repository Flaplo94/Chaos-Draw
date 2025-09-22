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
    [SerializeField] private float spreadDegrees = 0f;

    [Header("Refs")]
    public PlayerStats stats;

    [Header("Audio")]
    [SerializeField] private AudioClip attackSfx;
    [SerializeField] private AudioSource audioSource;

    [Header("Input System")]
    [SerializeField] private InputActionAsset inputActions;

    // Artifact hooks
    [HideInInspector] public int extraProjectiles = 0;
    [HideInInspector] public float fireRateMult = 1f;
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
    }

    void OnDisable()
    {
        if (attackAction != null)
            attackAction.Disable();
    }

    void Update()
    {
        if (cooldown > 0f) cooldown -= Time.unscaledDeltaTime;
        if (cooldown < 0f) cooldown = 0f;

        // Continuous fire while held: if button is pressed and CD is ready, shoot now.
        if (attackAction != null && attackAction.IsPressed())
        {
            TryFire();
        }
    }

    private void TryFire()
    {
        if (cooldown > 0f) return;
        if (dimmer != null && dimmer.dimmerOn) return;

        ThrowTowardMouse();

        if (attackSfx != null && audioSource != null)
            audioSource.PlayOneShot(attackSfx);

        float effective = baseCooldown / Mathf.Max(0.01f, fireRateMult);
        cooldown = effective;

        if (cooldownIndicator != null)
            cooldownIndicator.StartCooldown(effective, transform);
    }

    void ThrowTowardMouse()
    {
        if (!cardPrefab || !firePoint) return;

        // New Input System mouse position
        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(
            new Vector3(mouseScreen.x, mouseScreen.y, -Camera.main.transform.position.z)
        );
        mouseWorld.z = 0f;

        Vector2 dir = ((Vector2)(mouseWorld - firePoint.position)).normalized;
        SpawnCard(firePoint.position, dir);

        if (extraProjectiles > 0)
        {
            int half = extraProjectiles;
            for (int i = -half; i <= half; i++)
            {
                if (i == 0) continue;
                float angle = i * spreadDegrees;
                Vector2 d = (Quaternion.Euler(0, 0, angle) * dir).normalized;
                SpawnCard(firePoint.position, d);
            }
        }
    }

    void SpawnCard(Vector3 pos, Vector2 dir)
    {
        var go = Instantiate(cardPrefab, pos, Quaternion.identity);

        if (go.TryGetComponent<Bullet>(out var bullet))
        {
            int baseDmg = bullet.damage;
            float globalMult = (stats != null) ? stats.damageMult : 1f;
            float elemMult = 1f;

            bullet.debugBaseDamage = baseDmg;
            bullet.debugGlobalMult = globalMult;
            bullet.debugElementMult = elemMult;

            bullet.damage = Mathf.Max(1, Mathf.RoundToInt(baseDmg * globalMult));
        }

        if (go.TryGetComponent<Rigidbody2D>(out var rb))
            rb.linearVelocity = dir * (cardSpeed * Mathf.Max(0.01f, projectileSpeedMult));
    }
}
