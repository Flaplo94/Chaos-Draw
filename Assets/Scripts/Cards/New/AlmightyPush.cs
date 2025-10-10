using UnityEngine;

public class AlmightyPush : MonoBehaviour, IAbilityBehavior
{
    [Header("Push Settings")]
    [SerializeField] private float radius = 6f;
    [SerializeField] private float innerRadius = 0f;
    [SerializeField] private float pushForce = 18f;       // continuous force
    [SerializeField] private float impulseBoost = 0f;     // one-time impulse
    [SerializeField] private LayerMask enemyLayer;        // 0 = no filter
    [SerializeField] private float selfDestructAfter = 0.25f;

    [Header("Optional Stagger")]
    [SerializeField] private bool applyStun = false;
    [SerializeField] private float stunDuration = 0.15f;

    [Header("Lucky Shot (forward duplicate when owned)")]
    [SerializeField] private float duplicateForwardOffsetMin = 1.5f;
    [SerializeField] private float duplicateForwardOffsetScale = 1.15f;
    private bool luckyWasDuplicated = false;
    private Vector2 castDir = Vector2.right;

    [Header("Audio")]
    [SerializeField] private AudioClip impactSound;
    [SerializeField] private AudioSource audioSource;

    void Awake()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
            audioSource.clip = impactSound;
        }
    }

    // Matches your ability pattern: pass aim dir + rarity here
    public bool Initialize(Vector2 dir, Rarity rarity)
    {
        castDir = (dir.sqrMagnitude > 0.0001f) ? dir.normalized : Vector2.right;

        // RARITY SCALING — same idea as Fireball/AOEPulse/GodSpeed
        switch (rarity)
        {
            case Rarity.Uncommon:
                radius *= 1.10f; pushForce *= 1.10f; break;
            case Rarity.Rare:
                radius *= 1.25f; pushForce *= 1.20f; break;
            case Rarity.Epic:
                radius *= 1.40f; pushForce *= 1.30f; impulseBoost += 2f; break;
            case Rarity.Legendary:
                radius *= 1.60f; pushForce *= 1.40f; impulseBoost += 4f; applyStun = true; stunDuration = Mathf.Max(stunDuration, 0.2f); break;
        }
        return true;
    }

    private void Start()
    {
        PlayImpactSound();
        DoPush();

        // Lucky Shot: one forward “echo” push (mirrors AOEPulse forward-duplicate style)
        if (!luckyWasDuplicated)
        {
            LuckyShotSystem.OnSpellCast(() =>
            {
                Vector2 fwd = (castDir.sqrMagnitude > 0.0001f) ? castDir.normalized : Vector2.right;
                float fwdDist = Mathf.Max(duplicateForwardOffsetMin, radius * duplicateForwardOffsetScale);
                Vector3 p2 = transform.position + (Vector3)(fwd * fwdDist);

                var dup = Instantiate(gameObject, p2, transform.rotation);
                var comp = dup.GetComponent<AlmightyPush>();
                if (comp != null)
                {
                    comp.luckyWasDuplicated = true;
                    comp.castDir = this.castDir;
                }
            });
        }

        if (selfDestructAfter > 0f)
            Destroy(gameObject, selfDestructAfter);
    }

    private void DoPush()
    {
        float r = Mathf.Max(0f, radius);
        float inner = Mathf.Clamp(innerRadius, 0f, r);

        Collider2D[] hits = (enemyLayer.value == 0)
            ? Physics2D.OverlapCircleAll(transform.position, r)
            : Physics2D.OverlapCircleAll(transform.position, r, enemyLayer);

        if (hits == null || hits.Length == 0) return;

        Vector2 center = transform.position;

        foreach (var h in hits)
        {
            if (!h) continue;

            // Only affect actual enemies/bosses (consistent with your other abilities)
            bool isEnemy = h.TryGetComponent(out EnemyHealth eh);
            bool isBoss = h.TryGetComponent(out BossHealth bh);
            if (!isEnemy && !isBoss) continue;

            Vector2 delta = (Vector2)h.transform.position - center;
            float dist = delta.magnitude;
            if (dist < Mathf.Max(inner, 0.001f)) continue;

            Vector2 dir = delta / Mathf.Max(dist, 0.001f);

            var rb = h.attachedRigidbody;
            if (rb != null)
            {
                // light falloff so nearer targets get slightly more oomph
                float falloff = 1f - Mathf.Clamp01((dist - inner) / Mathf.Max(0.001f, (r - inner)));
                float applied = pushForce * (0.75f + 0.25f * falloff);

                rb.AddForce(dir * applied, ForceMode2D.Force);
                if (impulseBoost > 0f)
                    rb.AddForce(dir * impulseBoost, ForceMode2D.Impulse);
            }

            if (applyStun)
                StunReceiver.ApplyTo(h.transform, stunDuration); // same utility used in GodSpeed
        }
    }

    // Kept for consistency with abilities that drive SFX via Animation Events
    public void PlayImpactSound()
    {
        if (audioSource != null && impactSound != null)
            audioSource.Play();
    }

    // Not used here, but provided for parity with Fireball/AOEPulse
    public void OnImpactFinished() => Destroy(gameObject);

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.25f, 0.6f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, radius);

        if (innerRadius > 0f)
        {
            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, innerRadius);
        }
    }
#endif
}
