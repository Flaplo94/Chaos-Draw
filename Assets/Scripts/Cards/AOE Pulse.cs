using UnityEngine;
using UnityEngine.Audio;

public class AOEPulse : MonoBehaviour, IAbilityBehavior
{
    [Header("Tuning")]
    [SerializeField] private float radius = 3f;
    [SerializeField] private int damage = 5;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Audio")]
    [SerializeField] private AudioClip impactSound;
    [SerializeField] private AudioSource audioSource;
    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f; // 2D sound
        audioSource.clip = impactSound;
    }
    public bool Initialize(Vector2 dir, Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Uncommon: radius *= 1.1f; damage += 1; break;
            case Rarity.Rare: radius *= 1.25f; damage += 2; break;
            case Rarity.Epic: radius *= 1.4f; damage += 3; break;
            case Rarity.Legendary: radius *= 1.6f; damage += 5; break;
        }
        return true;
    }

    private void Start()
    {
        Collider2D[] hits = (enemyLayer.value == 0)
            ? Physics2D.OverlapCircleAll(transform.position, radius)
            : Physics2D.OverlapCircleAll(transform.position, radius, enemyLayer);

        // Fire element damage skal igennem DamageCalculator
        DamageResult result = DamageCalculator.ComputeFinalDamage(damage, DamageElement.Fire);

        foreach (var h in hits)
        {
            if (!h) continue;
            if (h.TryGetComponent(out EnemyHealth eh)) eh.TakeDamage(result.amount, result.element);
            if (h.TryGetComponent(out BossHealth bh)) bh.TakeDamage(result.amount, result.element);
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
        Gizmos.DrawWireSphere(transform.position, radius);
    }
#endif
}
