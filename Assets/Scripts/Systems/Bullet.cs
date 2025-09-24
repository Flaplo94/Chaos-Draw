using UnityEngine;

public class Bullet : MonoBehaviour
{
    public int baseDamage = 1;
    public float lifetime = 5f;

    [Header("Audio")]
    [SerializeField] private AudioClip hitSfx;
    [SerializeField] private AudioSource audioSource;

    // Back-compat property (nogle scripts bruger 'damage')
    public int damage { get => baseDamage; set => baseDamage = value; }

    [Header("Debug (for PlayerThrowing)")]
    [HideInInspector] public int debugBaseDamage = 0;
    [HideInInspector] public float debugGlobalMult = 1f;
    [HideInInspector] public float debugElementMult = 1f;

    [Header("Logging")]
    public bool logDamage = false;

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

    // Trigger-collision
    void OnTriggerEnter2D(Collider2D other) => HandleHit(other ? other.gameObject : null);

    // Physics-collision
    void OnCollisionEnter2D(Collision2D other) => HandleHit(other?.collider ? other.collider.gameObject : null);

    private void HandleHit(GameObject hit)
    {
        if (!hit) return;

        var enemy = hit.GetComponent<EnemyHealth>()
                 ?? hit.GetComponentInParent<EnemyHealth>()
                 ?? hit.GetComponentInChildren<EnemyHealth>();

        if (enemy != null)
        {
            var result = DamageCalculator.ComputeFinalDamage(baseDamage, DamageElement.Physical);
            enemy.TakeBasicAttackDamage(result.amount, result.element); // lifesteal for basic attacks
            StopAndVanish(); // <- vigtig: stop bev�gelse + skjul straks
            if (logDamage) LogDamage("Enemy", enemy.gameObject.name, result);

            // Yeet Cube: count basic-attack hit (enemy)
            YeetCubeSystem.ReportPlayerHit(enemy.transform.root, isBoss: false);
            return;
        }

        var boss = hit.GetComponent<BossHealth>()
                ?? hit.GetComponentInParent<BossHealth>()
                ?? hit.GetComponentInChildren<BossHealth>();

        if (boss != null)
        {
            var result = DamageCalculator.ComputeFinalDamage(baseDamage, DamageElement.Physical);
            boss.TakeDamage(result.amount, result.element);
            StopAndVanish(); // <- vigtig: stop bev�gelse + skjul straks

            // Yeet Cube: count basic-attack hit (boss)
            YeetCubeSystem.ReportPlayerHit(boss.transform.root, isBoss: true);

            if (logDamage) LogDamage("Boss", boss.gameObject.name, result);
            return;
        }
    }

    private void StopAndVanish()
    {
        // 1) Stop al bev�gelse
        if (rb)
        {
            rb.linearVelocity = Vector2.zero;     // sikrer stop
            rb.linearVelocity = Vector2.zero; // hvis projektet bruger linearVelocity
            rb.angularVelocity = 0f;
            rb.simulated = false;           // frys fysik
        }

        // 2) Disable ALLE colliders, s� kuglen ikke rammer flere ting
        foreach (var c in GetComponentsInChildren<Collider2D>()) c.enabled = false;

        // 3) Skjul visuelt (sprite/trail/particles)
        foreach (var sr in GetComponentsInChildren<SpriteRenderer>()) sr.enabled = false;
        foreach (var tr in GetComponentsInChildren<TrailRenderer>()) tr.emitting = false;
        foreach (var ps in GetComponentsInChildren<ParticleSystem>()) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        // 4) Afspil hit-lyd og ryd op
        if (hitSfx != null)
        {
            if (audioSource != null) audioSource.PlayOneShot(hitSfx);
            else AudioSource.PlayClipAtPoint(hitSfx, transform.position);
            Destroy(gameObject, hitSfx.length);
        }
        else
        {
            Destroy(gameObject); // ingen lyd, bare fjern med det samme
        }
    }

    private void LogDamage(string targetType, string targetName, DamageResult result)
    {
        Debug.Log(
            $"[DMG] {targetType} '{targetName}' <- {result.amount} " +
            $"(base {baseDamage}, mult={result.amount / (float)Mathf.Max(1, baseDamage):0.##}, element={result.element})"
        );
    }
}
