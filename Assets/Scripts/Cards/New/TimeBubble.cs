using UnityEngine;
using System.Collections.Generic;

public class TimeBubble : MonoBehaviour, IAbilityBehavior
{
    [Header("Core")]
    [SerializeField] private float duration = 4.0f;
    [SerializeField, Range(0.1f, 10f)] private float radius = 2.5f;
    [SerializeField, Range(0.1f, 1f)] private float slowMultiplier = 0.60f; // 0.6 = -40%

    [Header("Target Filters")]
    [Tooltip("Layers to affect (e.g., Enemies, EnemyProjectiles, PlayerProjectiles). Player is ignored by tag.")]
    [SerializeField] private LayerMask affectLayers = ~0;

    [Header("Optional FX")]
    [SerializeField] private SpriteRenderer circleVisual; // optional ring/area sprite
    [SerializeField] private AudioClip spawnSfx;
    [SerializeField] private AudioClip endSfx;
    [SerializeField] private AudioSource audioSource;

    private CircleCollider2D trigger;

    public bool Initialize(Vector2 _, Rarity rarity)
    {
        // duration scales slightly with rarity
        switch (rarity)
        {
            case Rarity.Uncommon: duration += 0.5f; break;
            case Rarity.Rare: duration += 1.0f; break;
            case Rarity.Epic: duration += 1.5f; break;
            case Rarity.Legendary: duration += 2.0f; break;
        }
        duration = Mathf.Max(0.05f, duration);
        return true;
    }

    private void Awake()
    {
        trigger = GetComponent<CircleCollider2D>();
        if (!trigger)
        {
            trigger = gameObject.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
        }
        trigger.radius = GetLocalColliderRadiusFromWorld(radius);
        trigger.offset = Vector2.zero;

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        // Respect instantiate position (Ability spawns us at mouse when the toggle is on)

        // --- SCALE VISUAL TO EXACT RADIUS (works with any sprite & parent scale) ---
        if (circleVisual != null && circleVisual.sprite != null)
        {
            var spr = circleVisual.sprite;

            // sprite.bounds.size is world size when localScale=(1,1,1)
            Vector2 spriteWorldSize = spr.bounds.size;
            float targetDiameterWorld = GetWorldRadius() * 2f;

            // Compensate for parent hierarchy scale so the final WORLD size = targetDiameterWorld
            var parent = circleVisual.transform.parent;
            Vector3 parentLossy = parent ? parent.lossyScale : Vector3.one;

            float denomX = Mathf.Max(0.0001f, spriteWorldSize.x * Mathf.Abs(parentLossy.x));
            float denomY = Mathf.Max(0.0001f, spriteWorldSize.y * Mathf.Abs(parentLossy.y));

            float scaleX = targetDiameterWorld / denomX;
            float scaleY = targetDiameterWorld / denomY;

            circleVisual.transform.localScale = new Vector3(scaleX, scaleY, 1f);
        }

        PlayOneShot(spawnSfx);
        if (duration > 0f) Invoke(nameof(DestroySelf), duration);

        var hits = new List<Collider2D>(16);
        var cf = new ContactFilter2D() { useTriggers = true };
        Physics2D.OverlapCircle(transform.position, GetWorldRadius(), cf, hits);
        for (int i = 0; i < hits.Count; i++)
        {
            var b = hits[i] ? hits[i].GetComponent<Bullet>() : null;
            if (b != null) b.AddExternalSpeedMultiplier(this, slowMultiplier);

            // NEW: also slow enemy bullets already inside
            var eb = hits[i] ? hits[i].GetComponent<EnemyBullet>() : null;
            if (eb != null) eb.AddExternalSpeedMultiplier(this, slowMultiplier);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var bullet = other.GetComponent<Bullet>();
        if (bullet != null)
        {
            bullet.AddExternalSpeedMultiplier(this, slowMultiplier);
            return; // do NOT add TimeBubbleEffector to bullets
        }

        // NEW: enemy bullet support
        var enemyBullet = other.GetComponent<EnemyBullet>();
        if (enemyBullet != null)
        {
            enemyBullet.AddExternalSpeedMultiplier(this, slowMultiplier);
            return; // do NOT add TimeBubbleEffector to bullets
        }

        if (!ShouldAffect(other)) return;

        var effector = other.GetComponent<TimeBubbleEffector>();
        if (!effector) effector = other.gameObject.AddComponent<TimeBubbleEffector>();
        effector.AddSource(this, slowMultiplier);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        var bullet = other.GetComponent<Bullet>();
        if (bullet != null)
        {
            bullet.RemoveExternalSpeedMultiplier(this);
            return; // nothing else for bullets
        }

        // NEW: enemy bullet support
        var enemyBullet = other.GetComponent<EnemyBullet>();
        if (enemyBullet != null)
        {
            enemyBullet.RemoveExternalSpeedMultiplier(this);
            return; // nothing else for bullets
        }

        if (!other) return;
        var effector = other.GetComponent<TimeBubbleEffector>();
        if (effector) effector.RemoveSource(this);
    }

    private bool ShouldAffect(Collider2D other)
    {
        if (!other) return false;
        if (other.CompareTag("Player")) return false;

        // Layer filter
        if (affectLayers != ~0)
        {
            int bit = 1 << other.gameObject.layer;
            if ((affectLayers.value & bit) == 0) return false;
        }
        return true;
    }

    private void DestroySelf()
    {
        // Proactively remove our source from anything still inside
        var results = new List<Collider2D>(16);
        var cf = new ContactFilter2D() { useTriggers = true };
        Physics2D.OverlapCircle(transform.position, GetWorldRadius() + 0.05f, cf, results);
        for (int i = 0; i < results.Count; i++)
        {
            var b = results[i] ? results[i].GetComponent<Bullet>() : null;
            if (b != null)
            {
                b.RemoveExternalSpeedMultiplier(this);
                continue; // skip to next collider
            }

            // NEW: enemy bullet support
            var eb = results[i] ? results[i].GetComponent<EnemyBullet>() : null;
            if (eb != null)
            {
                eb.RemoveExternalSpeedMultiplier(this);
                continue;
            }

            var eff = results[i] ? results[i].GetComponent<TimeBubbleEffector>() : null;
            if (eff) eff.RemoveSource(this);
        }

        CancelInvoke();
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        PlayOneShot(endSfx);
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (!clip) return;
        if (audioSource) audioSource.PlayOneShot(clip);
        else
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            var pAS = p ? p.GetComponent<AudioSource>() : null;
            if (pAS) pAS.PlayOneShot(clip);
        }
    }
    private float GetMaxLossyScale()
    {
        var s = transform.lossyScale;
        return Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y));
    }
    private float GetLocalColliderRadiusFromWorld(float worldRadius) => worldRadius / Mathf.Max(0.0001f, GetMaxLossyScale());
    private float GetWorldRadius() => radius;

    private void OnValidate()
    {
        if (!trigger) trigger = GetComponent<CircleCollider2D>();
        if (trigger) trigger.radius = GetLocalColliderRadiusFromWorld(radius);

        if (circleVisual != null && circleVisual.sprite != null)
        {
            // Mirror the same math as in Start()
            var spr = circleVisual.sprite;
            Vector2 spriteWorldSize = spr.bounds.size;
            float targetDiameterWorld = radius * 2f;
            var parent = circleVisual.transform.parent;
            Vector3 parentLossy = parent ? parent.lossyScale : Vector3.one;

            float denomX = Mathf.Max(0.0001f, spriteWorldSize.x * Mathf.Abs(parentLossy.x));
            float denomY = Mathf.Max(0.0001f, spriteWorldSize.y * Mathf.Abs(parentLossy.y));
            circleVisual.transform.localScale = new Vector3(
                targetDiameterWorld / denomX,
                targetDiameterWorld / denomY,
                1f
            );
        }
    }

}
