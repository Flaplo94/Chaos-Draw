using UnityEngine;

public class AOEPulse : MonoBehaviour, IAbilityBehavior
{
    [Header("Tuning")]
    [SerializeField] private float radius = 3f;
    [SerializeField] private int damage = 5;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Lucky Shot (forward duplicate when owned)")]
    [Tooltip("Minimum forward distance (world units) in the aim direction for the duplicate.")]
    [SerializeField] private float duplicateForwardOffsetMin = 1.5f;

    [Tooltip("Extra forward distance based on radius. Final = max(Min, radius * Scale).")]
    [SerializeField] private float duplicateForwardOffsetScale = 1.15f;

    // Prevent the duplicate from duplicating again
    private bool luckyWasDuplicated = false;

    // Saved aim so we can place the duplicate forward
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
            audioSource.spatialBlend = 0f; // 2D
            audioSource.clip = impactSound;
        }
    }

    public bool Initialize(Vector2 dir, Rarity rarity)
    {
        // Store aim (fallback to world-right if none)
        castDir = (dir.sqrMagnitude > 0.0001f) ? dir.normalized : Vector2.right;

        // Your rarity scaling
        switch (rarity)
        {
            case Rarity.Uncommon: radius *= 1.10f; damage += 1; break;
            case Rarity.Rare: radius *= 1.25f; damage += 2; break;
            case Rarity.Epic: radius *= 1.40f; damage += 3; break;
            case Rarity.Legendary: radius *= 1.60f; damage += 5; break;
        }
        return true;
    }

    private void Start()
    {
        // 1) Apply the pulse damage immediately
        Collider2D[] hits = (enemyLayer.value == 0)
            ? Physics2D.OverlapCircleAll(transform.position, radius)
            : Physics2D.OverlapCircleAll(transform.position, radius, enemyLayer);

        DamageResult result = DamageCalculator.ComputeFinalDamage(damage, DamageElement.Fire);
        foreach (var h in hits)
        {
            if (!h) continue;
            if (h.TryGetComponent(out EnemyHealth eh)) eh.TakeDamage(result.amount, result.element);
            if (h.TryGetComponent(out BossHealth bh)) bh.TakeDamage(result.amount, result.element);
        }

        // 2) Lucky Shot duplicate (FORWARD in aim direction)
        if (!luckyWasDuplicated)
        {
            LuckyShotSystem.OnSpellCast(() =>
            {
                Vector2 fwd = (castDir.sqrMagnitude > 0.0001f) ? castDir.normalized : Vector2.right;

                // Bigger forward distance for AOEPulse
                float fwdDist = Mathf.Max(duplicateForwardOffsetMin, radius * duplicateForwardOffsetScale);

                Vector3 p2 = transform.position + (Vector3)(fwd * fwdDist);

                var dup = Instantiate(gameObject, p2, transform.rotation);
                var comp = dup.GetComponent<AOEPulse>();
                if (comp != null)
                {
                    comp.luckyWasDuplicated = true; // don’t chain
                    comp.castDir = this.castDir;      // keep same aim
                }
            });
        }
    }

    // If you use a VFX that ends via animation event, keep this hook.
    public void OnImpactFinished() => Destroy(gameObject);

    public void PlayImpactSound()
    {
        if (audioSource != null && impactSound != null) audioSource.Play();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
#endif
}
