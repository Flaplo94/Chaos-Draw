using UnityEngine;
using System.Collections.Generic;

public class ChainLightning : MonoBehaviour, IAbilityBehavior
{
    [Header("Tuning")]
    [SerializeField] private float range = 5f;
    [SerializeField] private int damage = 1;
    [SerializeField] private int maxChains = 3;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Visuals")]
    [SerializeField] private GameObject castVisual;
    [SerializeField] private GameObject lightningVisual;
    [SerializeField] private string playTrigger = "Play";
    [SerializeField] private string stateName = "Zap";
    [SerializeField] private float widthScale = 1f;

    [Header("Stun")]
    [SerializeField] private bool applyStun = true;
    [SerializeField] private float stunDuration = 0.5f;

    private bool luckyWasDuplicated = false;

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

            if (lightningVisual)
                SpawnBolt(prevPos, nearest.transform.position);

            var result = DamageCalculator.ComputeFinalDamage(damage, DamageElement.Lightning);

            if (nearest.TryGetComponent(out EnemyHealth eh)) eh.TakeDamage(result.amount, result.element);
            if (nearest.TryGetComponent(out BossHealth bh)) bh.TakeDamage(result.amount, result.element);

            if (applyStun)
            {
                Transform root = nearest.attachedRigidbody ? nearest.attachedRigidbody.transform : nearest.transform;
                StunReceiver.ApplyTo(root, stunDuration);
            }

            hitSet.Add(nearest);
            prevPos = nearest.transform.position;
        }

        if (!luckyWasDuplicated)
        {
            LuckyShotSystem.OnSpellCast(this, () =>
            {
                var p2 = transform.position;
                if (LuckyShotSystem.TryConsumeSpawnOffset(out var off)) p2 += (Vector3)off;

                var dup = Instantiate(gameObject, p2, transform.rotation);
                var comp = dup.GetComponent<ChainLightning>();
                if (comp != null) comp.luckyWasDuplicated = true;
            });
        }

        Destroy(gameObject);
    }

    private void SpawnBolt(Vector3 from, Vector3 to)
    {
        var go = Instantiate(lightningVisual);
        go.name = "ChainLightning_Bolt";

        Vector3 mid = (from + to) * 0.5f;
        go.transform.position = mid;

        Vector2 dir = (to - from);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        go.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        var sr = go.GetComponentInChildren<SpriteRenderer>();
        float length = dir.magnitude;
        if (sr && sr.sprite)
        {
            float currentWidth = sr.bounds.size.x;
            if (currentWidth <= 0f)
                currentWidth = sr.sprite.rect.width / sr.sprite.pixelsPerUnit * go.transform.lossyScale.x;

            if (currentWidth > 0f)
            {
                float scaleMul = length / currentWidth;
                go.transform.localScale = new Vector3(go.transform.localScale.x * scaleMul,
                                                       go.transform.localScale.y * widthScale,
                                                       go.transform.localScale.z);
            }
            else go.transform.localScale = new Vector3(length, widthScale, 1f);
        }
        else go.transform.localScale = new Vector3(length, widthScale, 1f);

        var anim = go.GetComponentInChildren<Animator>();
        float ttl = 0.2f;
        if (anim)
        {
            anim.Rebind();
            anim.Update(0f);
            if (!string.IsNullOrEmpty(playTrigger)) anim.SetTrigger(playTrigger);
            else if (!string.IsNullOrEmpty(stateName)) anim.Play(stateName, 0, 0f);
            ttl = GetAnimatorApproxLength(anim, stateName);
            if (ttl <= 0f) ttl = 0.25f;
        }
        Destroy(go, ttl);
    }

    private static float GetAnimatorApproxLength(Animator anim, string preferredStateName)
    {
        if (!anim || anim.runtimeAnimatorController == null) return 0f;
        var st = anim.GetCurrentAnimatorStateInfo(0);
        if (st.length > 0.0001f) return st.length;

        float best = 0f;
        var clips = anim.runtimeAnimatorController.animationClips;
        if (!string.IsNullOrEmpty(preferredStateName))
        {
            foreach (var c in clips) if (c && c.name == preferredStateName) return c.length;
        }
        foreach (var c in clips) if (c && c.length > best) best = c.length;
        return best;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, range);
    }
#endif
}
