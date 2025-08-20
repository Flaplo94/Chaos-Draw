using UnityEngine;

public class PlayerThrowing : MonoBehaviour
{
    [Header("Projectile")]
    public GameObject cardPrefab;   // din "Bullet"-prefab
    public Transform firePoint;
    public float cardSpeed = 10f;

    [Header("Fire")]
    public float baseCooldown = 0.30f;
    [SerializeField] private float spreadDegrees = 0f; // 0 = lige ud; sæt >0 hvis du får ekstra projektiler senere

    [Header("Refs")]
    public PlayerStats stats;   // DRAG PlayerStats fra Player ind her i Inspector

    // Artifact hooks (sættes af ArtifactSystem)
    [HideInInspector] public int extraProjectiles = 0;
    [HideInInspector] public float fireRateMult = 1f;        // Gamers Cap kan øge denne
    [HideInInspector] public float projectileSpeedMult = 1f; // Gamers Cap kan øge denne

    private float cooldown;

    void Update()
    {
        if (cooldown > 0f) cooldown -= Time.unscaledDeltaTime; // bruger unscaled hvis shoppen pauser Time.timeScale
        if (cooldown < 0f) cooldown = 0f;

        if (Input.GetMouseButton(0) && cooldown <= 0f)
        {
            ThrowTowardMouse();
            float effective = baseCooldown / Mathf.Max(0.01f, fireRateMult);
            cooldown = effective;
        }
    }

    void ThrowTowardMouse()
    {
        if (!cardPrefab || !firePoint) return;

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

        // ----- VIGTIG DEL: skaler Bullet.damage med global damage multiplier -----
        if (go.TryGetComponent<Bullet>(out var bullet))
        {
            int baseDmg = bullet.damage; // værdi fra prefab
            float mult = (stats != null) ? stats.damageMult : 1f;
            bullet.damage = Mathf.Max(1, Mathf.RoundToInt(baseDmg * mult));
        }

        if (go.TryGetComponent<Rigidbody2D>(out var rb))
        {
            rb.linearVelocity = dir * (cardSpeed * Mathf.Max(0.01f, projectileSpeedMult));
        }
    }
}
