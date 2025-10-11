using UnityEngine;
using System.Collections;
public class AlmightyPush : MonoBehaviour, IAbilityBehavior
{
    [Header("Push Settings")]
    [SerializeField] private float range = 6f;
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
                range *= 1.10f; pushForce *= 1.10f; break;
            case Rarity.Rare:
                range *= 1.25f; pushForce *= 1.20f; break;
            case Rarity.Epic:
                range *= 1.40f; pushForce *= 1.30f; impulseBoost += 2f; break;
            case Rarity.Legendary:
                range *= 1.60f; pushForce *= 1.40f; impulseBoost += 4f; applyStun = true; stunDuration = Mathf.Max(stunDuration, 0.2f); break;
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
                float fwdDist = Mathf.Max(duplicateForwardOffsetMin, range * duplicateForwardOffsetScale);
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
        float r = Mathf.Max(0f, range);
        float inner = Mathf.Clamp(innerRadius, 0f, r);

        Collider2D[] hits = (enemyLayer.value == 0)
            ? Physics2D.OverlapCircleAll(transform.position, r)
            : Physics2D.OverlapCircleAll(transform.position, r, enemyLayer);

        if (hits == null || hits.Length == 0)
        {
            Debug.Log("[AlmightyPush] No targets found in radius " + r);
            return;
        }

        Vector2 center = transform.position;
        Debug.Log($"[AlmightyPush] Pushing {hits.Length} colliders within radius {r}");

        foreach (var h in hits)
        {
            if (!h) continue;

            // Accept anything with enemy tag or movement scripts
            bool hasFollow = h.TryGetComponent<EnemyFollow>(out _);
            bool hasFly = h.TryGetComponent<FlyingEnemy>(out _);
            bool tagged = h.CompareTag("Enemy");

            if (!hasFollow && !hasFly && !tagged)
                continue;

            Vector2 delta = (Vector2)h.transform.position - center;
            float dist = delta.magnitude;
            if (dist < Mathf.Max(inner, 0.001f)) continue;

            Vector2 dir = delta / Mathf.Max(dist, 0.001f);

            // Calculate distance based on radius/pushForce
            float falloff = 1f - Mathf.Clamp01((dist - inner) / Mathf.Max(0.001f, (r - inner)));
            float shoveDistance = Mathf.Clamp(pushForce * falloff * 0.15f, 1.5f, 10f);

            // Smooth movement over a few frames so it's visible
            StartCoroutine(KnockbackTransform(h.transform, dir, shoveDistance, 0.10f));

            if (applyStun)
                StartCoroutine(DelayedStun(h.transform, stunDuration, 0.12f));
        }
    }


    private IEnumerator KnockbackTransform(Transform target, Vector2 dir, float distance, float duration)
    {
        if (target == null) yield break;

        Vector3 start = target.position;
        Vector3 end = start + (Vector3)(dir.normalized * distance);

        float t = 0f;
        while (t < duration && target != null)
        {
            t += Time.deltaTime;
            float alpha = t / Mathf.Max(0.0001f, duration);
            // Ease-out
            alpha = 1f - (1f - alpha) * (1f - alpha);
            target.position = Vector3.Lerp(start, end, alpha);
            yield return null;
        }

        if (target != null) target.position = end;
    }

    private IEnumerator DelayedStun(Transform targetRoot, float duration, float delay)
    {
        if (targetRoot == null) yield break;
        if (delay > 0f) yield return new WaitForSeconds(delay);
        StunReceiver.ApplyTo(targetRoot, duration);
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
        Gizmos.DrawWireSphere(transform.position, range);

        if (innerRadius > 0f)
        {
            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, innerRadius);
        }
    }
#endif
}
