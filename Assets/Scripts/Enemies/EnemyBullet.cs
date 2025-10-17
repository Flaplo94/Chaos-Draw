using UnityEngine;
using System.Collections.Generic;

public class EnemyBullet : MonoBehaviour
{
    public int damage = 1;
    public float lifetime = 5f;

    // --- NEW: slowdown support like Bullet.cs ---
    private Rigidbody2D rb;
    private readonly Dictionary<object, float> _externalSpeedMults = new Dictionary<object, float>();
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
    // --- END NEW ---

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        Destroy(gameObject, lifetime); // Clean up bullet after a few seconds
    }

    private void FixedUpdate()
    {
        if (!rb || !rb.simulated) return;

        // Same pattern as Bullet.cs: apply the *change* in multiplier to velocity
        float targetMult = (_externalSpeedMults.Count == 0)
            ? 1f
            : Mathf.Max(minExternalSpeedMult, GetCombinedExternalSpeedMultiplier());

        if (!Mathf.Approximately(targetMult, _lastAppliedExternalMult))
        {
            float safeLast = (_lastAppliedExternalMult <= 0.0001f) ? 1f : _lastAppliedExternalMult;
            float factor = targetMult / safeLast;
            rb.linearVelocity *= factor;
            _lastAppliedExternalMult = targetMult;
        }
    }

    void Update()
    {
        // Optional: align sprite to velocity like your player Bullet does
        if (rb && rb.linearVelocity.sqrMagnitude > 0.01f)
        {
            float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle - 90f, Vector3.forward);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        PlayerHealth player = other.GetComponent<PlayerHealth>();
        if (player != null)
        {
            player.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}
