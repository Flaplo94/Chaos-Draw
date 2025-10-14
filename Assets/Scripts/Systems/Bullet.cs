using UnityEngine;
using System.Collections.Generic;

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

    // — external slow registry for zones like Time Bubble —
    private readonly Dictionary<object, float> _externalSpeedMults
        = new Dictionary<object, float>();
    [SerializeField] private float minExternalSpeedMult = 0.10f; // floor so bullets never freeze
    private float _lastAppliedExternalMult = 1f;
    public void AddExternalSpeedMultiplier(object owner, float multiplier)
    {
        if (owner == null) return;
        _externalSpeedMults[owner] = Mathf.Clamp01(multiplier);
    }
    public void RemoveExternalSpeedMultiplier(object owner)
    {
        if (owner == null) return;
        _externalSpeedMults.Remove(owner);
    }
    private float GetCombinedExternalSpeedMultiplier()
    {
        float m = 1f;
        foreach (var kv in _externalSpeedMults) m *= kv.Value;
        return Mathf.Clamp01(m);
    }


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

    private void FixedUpdate()
    {
        if (!rb || !rb.simulated) return;

        // Compute target external multiplier. When no sources -> 1f (restore speed).
        float targetMult = (_externalSpeedMults.Count == 0)
            ? 1f
            : Mathf.Max(minExternalSpeedMult, GetCombinedExternalSpeedMultiplier());

        // If multiplier changed since last tick, scale velocity by the delta (NOT compounding).
        if (!Mathf.Approximately(targetMult, _lastAppliedExternalMult))
        {
            // Handle the rare case where _lastAppliedExternalMult == 0 (shouldn’t happen with our floor).
            float safeLast = (_lastAppliedExternalMult <= 0.0001f) ? 1f : _lastAppliedExternalMult;
            float factor = targetMult / safeLast;

            rb.linearVelocity *= factor;
            _lastAppliedExternalMult = targetMult;
        }
    }


    // Trigger-collision
    void OnTriggerEnter2D(Collider2D other) => HandleHit(other ? other.gameObject : null);

    // Physics-collision
    void OnCollisionEnter2D(Collision2D other) => HandleHit(other?.collider ? other.collider.gameObject : null);

    private void HandleHit(GameObject hit)
    {
        if (!hit) return;

        var enemy = hit.GetComponent<EnemyHealth>();

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

        var boss = hit.GetComponent<BossHealth>();

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
