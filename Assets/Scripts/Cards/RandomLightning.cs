using UnityEngine;
using System.Collections.Generic;

public class RandomLightning : MonoBehaviour, IAbilityBehavior
{
    [Header("Targeting")]
    [SerializeField] private float radius = 6f;

    [Header("Strikes")]
    [SerializeField] private int minStrikes = 1;
    [SerializeField] private int maxStrikes = 2;
    [SerializeField] private int damage = 2;

    [Header("Visual (Animator + SpriteRenderer)")]
    [Tooltip("Your LightningStrikeVisual prefab (has SpriteRenderer + Animator).")]
    [SerializeField] private GameObject lightningVisual;
    [Tooltip("Animator trigger to play on the strike (leave empty to use a state name).")]
    [SerializeField] private string playTrigger = "Play";
    [Tooltip("Fallback state to play if no trigger is used (e.g., 'Strike').")]
    [SerializeField] private string stateName = "Strike";
    [Tooltip("Animation speed multiplier for the strike.")]
    [SerializeField] private float animSpeed = 1.0f;
    [Tooltip("Optional local offset to place strike slightly above/around target.")]
    [SerializeField] private Vector2 strikeOffset = new Vector2(0f, 0.0f);

    // rarity scaling
    private int bonusStrikes = 0;
    private float damageMultiplier = 1f;

    public bool Initialize(Vector2 _, Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Uncommon: bonusStrikes = 1; damageMultiplier = 1.10f; break;
            case Rarity.Rare: bonusStrikes = 2; damageMultiplier = 1.20f; break;
            case Rarity.Epic: bonusStrikes = 3; damageMultiplier = 1.30f; break;
            case Rarity.Legendary: bonusStrikes = 4; damageMultiplier = 1.40f; break;
        }
        return true;
    }

    private void Start()
    {
        // Collect valid targets (works for enemies AND bosses by component)
        Collider2D[] inRange = Physics2D.OverlapCircleAll(transform.position, radius);
        List<Collider2D> valid = new List<Collider2D>(inRange.Length);

        for (int i = 0; i < inRange.Length; i++)
        {
            var c = inRange[i];
            if (!c) continue;
            if (c.GetComponent<EnemyHealth>() != null || c.GetComponent<BossHealth>() != null)
                valid.Add(c);
        }

        if (valid.Count == 0) { Destroy(gameObject); return; }

        int adjMin = Mathf.Max(0, minStrikes + bonusStrikes);
        int adjMax = Mathf.Max(adjMin, maxStrikes + bonusStrikes);
        int strikeCount = Mathf.Clamp(Random.Range(adjMin, adjMax + 1), 0, valid.Count);

        int baseAdj = Mathf.Max(1, Mathf.RoundToInt(damage * damageMultiplier));
        int finalDamage = DamageCalculator.ComputeFinalDamage(baseAdj, DamageElement.Lightning);

        for (int i = 0; i < strikeCount; i++)
        {
            int idx = Random.Range(0, valid.Count);
            var targetCol = valid[idx];
            valid.RemoveAt(idx);
            if (!targetCol) continue;

            Transform t = targetCol.attachedRigidbody ? targetCol.attachedRigidbody.transform : targetCol.transform;

            // Deal damage (enemy or boss)
            var eh = targetCol.GetComponent<EnemyHealth>();
            if (eh != null) eh.TakeDamage(finalDamage);
            var bh = targetCol.GetComponent<BossHealth>();
            if (bh != null) bh.TakeDamage(finalDamage);

            // Spawn strike visual at target (no LineRenderer)
            if (lightningVisual != null)
                SpawnStrikeAt(t.position + (Vector3)strikeOffset);
        }

        Destroy(gameObject);
    }

    private void SpawnStrikeAt(Vector3 pos)
    {
        var go = Instantiate(lightningVisual, pos, Quaternion.identity);
        go.name = "RandomLightning_Strike";

        // Play animation
        float ttl = 0.2f; // fallback
        var anim = go.GetComponentInChildren<Animator>();
        if (anim)
        {
            anim.speed = Mathf.Max(0.01f, animSpeed);
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

            // Try to destroy after current state's length / speed
            var st = anim.GetCurrentAnimatorStateInfo(0);
            ttl = st.length > 0f ? st.length / anim.speed : 0.25f;
        }

        Destroy(go, ttl);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.6f, 0.9f, 1f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
#endif
}
