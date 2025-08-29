using UnityEngine;
using System.Collections.Generic;

public class ChainLightning : MonoBehaviour, IAbilityBehavior
{
    [Header("Tuning")]
    [SerializeField] private float range = 5f;
    [SerializeField] private int damage = 1;
    [SerializeField] private int maxChains = 3;
    [SerializeField] private LayerMask enemyLayer;    // include Boss layer here too

    [Header("Visuals")]
    [SerializeField] private GameObject castVisual;       // optional one-shot VFX at the start
    [SerializeField] private GameObject lightningVisual;  // prefab with SpriteRenderer + Animator
    [SerializeField] private string playTrigger = "Play"; // Animator trigger on lightningVisual
    [SerializeField] private string stateName = "Zap";    // fallback state if you don't use a trigger
    [SerializeField] private float widthScale = 1f;       // visual thickness multiplier

    public bool Initialize(Vector2 dir, Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Uncommon: maxChains += 1; damage += 1; break;
            case Rarity.Rare: maxChains += 2; damage += 2; break;
            case Rarity.Epic: maxChains += 3; damage += 3; break;
            case Rarity.Legendary: maxChains += 5; damage += 4; break;
        }
        return true;
    }

    private void Start()
    {
        if (castVisual)
        {
            var cast = Instantiate(castVisual, transform.position, Quaternion.identity);
            Destroy(cast, 0.5f);
        }

        // If mask is zero, don't filter; otherwise respect mask.
        Collider2D[] inRange = (enemyLayer.value == 0)
            ? Physics2D.OverlapCircleAll(transform.position, range)
            : Physics2D.OverlapCircleAll(transform.position, range, enemyLayer);

        var hitSet = new HashSet<Collider2D>();
        Vector3 prevPos = transform.position;

        for (int i = 0; i < maxChains; i++)
        {
            Collider2D nearest = null;
            float nearestDist = float.MaxValue;

            foreach (var c in inRange)
            {
                if (!c || hitSet.Contains(c)) continue;
                float dist = Vector2.Distance(prevPos, c.transform.position);
                if (dist < nearestDist)
                {
                    nearest = c;
                    nearestDist = dist;
                }
            }

            if (!nearest) break;

            // 1) Spawn a visual bolt between prevPos and nearest
            if (lightningVisual)
                SpawnBolt(prevPos, nearest.transform.position);

            // 2) Apply damage (handles Enemy or Boss)
            if (nearest.TryGetComponent(out EnemyHealth eh)) eh.TakeDamage(damage);
            if (nearest.TryGetComponent(out BossHealth bh)) bh.TakeDamage(damage);

            // 3) Continue chaining from the last hit
            hitSet.Add(nearest);
            prevPos = nearest.transform.position;
        }

        Destroy(gameObject);
    }

    private void SpawnBolt(Vector3 from, Vector3 to)
    {
        var go = Instantiate(lightningVisual);
        go.name = "ChainLightning_Bolt";

        // Position at midpoint
        Vector3 mid = (from + to) * 0.5f;
        go.transform.position = mid;

        // Rotate to face target
        Vector2 dir = (to - from);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        go.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        // Scale length to match segment distance using the SpriteRenderer bounds
        var sr = go.GetComponentInChildren<SpriteRenderer>();
        float length = dir.magnitude;
        if (sr && sr.sprite)
        {
            // Compute current world width of the sprite along X
            // If bounds aren't ready this frame, estimate from sprite rect/PPU.
            float currentWidth = sr.bounds.size.x;
            if (currentWidth <= 0f)
                currentWidth = sr.sprite.rect.width / sr.sprite.pixelsPerUnit * go.transform.lossyScale.x;

            if (currentWidth > 0f)
            {
                float scaleMul = length / currentWidth;
                // Scale along X (length) and optionally thicken Y
                go.transform.localScale = new Vector3(go.transform.localScale.x * scaleMul,
                                                       go.transform.localScale.y * widthScale,
                                                       go.transform.localScale.z);
            }
            else
            {
                // Fallback: assume 1 world unit base width
                go.transform.localScale = new Vector3(length, widthScale, 1f);
            }
        }
        else
        {
            // No SR? Still place/rotate so any child FX line up
            go.transform.localScale = new Vector3(length, widthScale, 1f);
        }

        // Kick the animation
        var anim = go.GetComponentInChildren<Animator>();
        float ttl = 0.2f; // fallback lifetime
        if (anim)
        {
            anim.Rebind();
            anim.Update(0f);
            if (!string.IsNullOrEmpty(playTrigger))
            {
                anim.ResetTrigger(playTrigger);
                anim.SetTrigger(playTrigger);
            }
            else if (!string.IsNullOrEmpty(stateName))
            {
                anim.Play(stateName, 0, 0f);
            }
            // Try to auto-destroy after clip length
            ttl = GetAnimatorApproxLength(anim, stateName);
            if (ttl <= 0f) ttl = 0.25f;
        }

        Destroy(go, ttl);
    }

    private static float GetAnimatorApproxLength(Animator anim, string preferredStateName)
    {
        if (!anim || anim.runtimeAnimatorController == null) return 0f;

        // If currently in a state with a valid length, use it
        var st = anim.GetCurrentAnimatorStateInfo(0);
        if (st.length > 0.0001f) return st.length;

        // Otherwise, use the longest clip in the controller, or the named one if provided
        float best = 0f;
        var clips = anim.runtimeAnimatorController.animationClips;
        if (!string.IsNullOrEmpty(preferredStateName))
        {
            foreach (var c in clips)
                if (c && c.name == preferredStateName) return c.length;
        }
        foreach (var c in clips)
            if (c && c.length > best) best = c.length;

        return best;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.6f, 0.9f, 1f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, range);
    }
#endif
}
