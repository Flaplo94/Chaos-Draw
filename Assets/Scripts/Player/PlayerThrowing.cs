using UnityEngine;

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
    [SerializeField] private AudioClip attackSfx;  // <-- assign in Inspector
    [SerializeField] private AudioSource audioSource; // <-- assign in Inspector (or GetComponent in Awake)

    // Artifact hooks
    [HideInInspector] public int extraProjectiles = 0;
    [HideInInspector] public float fireRateMult = 1f;
    [HideInInspector] public float projectileSpeedMult = 1f;

    float cooldown;

    [SerializeField] private AttackCooldownIndicator cooldownIndicator; // assign prefab in inspector

    private Dimmer dimmer;

    void Awake()
    {
        dimmer = FindFirstObjectByType<Dimmer>();
        // fallback if not assigned
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (cooldown > 0f) cooldown -= Time.unscaledDeltaTime;
        if (cooldown < 0f) cooldown = 0f;

        if (Input.GetMouseButton(0) && cooldown <= 0f)
        {
            if (dimmer != null && !dimmer.dimmerOn)
            {

                ThrowTowardMouse();

                // play attack sound
                if (attackSfx != null && audioSource != null)
                    audioSource.PlayOneShot(attackSfx);

                float effective = baseCooldown / Mathf.Max(0.01f, fireRateMult);
                cooldown = effective;
                if (cooldownIndicator != null)
                {
                    cooldownIndicator.StartCooldown(effective, transform);
                }
                else
                {
                    Debug.LogWarning("[PlayerThrowing] cooldownIndicator not assigned!");
                }
            }
        }

    }

    void ThrowTowardMouse()
    {
        if (!cardPrefab || !firePoint || Camera.main == null) return;

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
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
