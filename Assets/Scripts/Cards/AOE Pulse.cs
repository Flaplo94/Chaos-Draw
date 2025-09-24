using UnityEngine;

public class AOEPulse : MonoBehaviour, IAbilityBehavior
{
    [Header("Tuning")]
    [SerializeField] private float range = 3f;
    [SerializeField] private int damage = 5;
    [SerializeField] private LayerMask enemyLayer;

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

    public bool Initialize(Vector2 dir, Rarity rarity)
    {
        castDir = (dir.sqrMagnitude > 0.0001f) ? dir.normalized : Vector2.right;

        switch (rarity)
        {
            case Rarity.Uncommon: range *= 1.10f; damage += 1; break;
            case Rarity.Rare: range *= 1.25f; damage += 2; break;
            case Rarity.Epic: range *= 1.40f; damage += 3; break;
            case Rarity.Legendary: range *= 1.60f; damage += 5; break;
        }
        return true;
    }

    private void Start()
    {
        Collider2D[] hits = (enemyLayer.value == 0)
            ? Physics2D.OverlapCircleAll(transform.position, range)
            : Physics2D.OverlapCircleAll(transform.position, range, enemyLayer);

        DamageResult result = DamageCalculator.ComputeFinalDamage(damage, DamageElement.Fire);
        foreach (var h in hits)
        {
            if (!h) continue;
            if (h.TryGetComponent(out EnemyHealth eh))
            {
                eh.TakeDamage(result.amount, result.element);
                BurnRules.TryApplyBurn(h.transform, result.amount);
            }
            if (h.TryGetComponent(out BossHealth bh))
            {
                bh.TakeDamage(result.amount, result.element);
                BurnRules.TryApplyBurn(h.transform, result.amount);
            }
        }

        if (!luckyWasDuplicated)
        {
            LuckyShotSystem.OnSpellCast(() =>
            {
                Vector2 fwd = (castDir.sqrMagnitude > 0.0001f) ? castDir.normalized : Vector2.right;
                float fwdDist = Mathf.Max(duplicateForwardOffsetMin, range * duplicateForwardOffsetScale);
                Vector3 p2 = transform.position + (Vector3)(fwd * fwdDist);

                var dup = Instantiate(gameObject, p2, transform.rotation);
                var comp = dup.GetComponent<AOEPulse>();
                if (comp != null)
                {
                    comp.luckyWasDuplicated = true;
                    comp.castDir = this.castDir;
                }
            });
        }
    }

    public void OnImpactFinished() => Destroy(gameObject);

    public void PlayImpactSound()
    {
        if (audioSource != null && impactSound != null) audioSource.Play();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, range);
    }
#endif
}
