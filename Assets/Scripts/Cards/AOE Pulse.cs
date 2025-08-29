using System.Collections.Generic;
using UnityEngine;

public class AOEPulse : MonoBehaviour, IAbilityBehavior
{
    [Header("Effect")]
    [SerializeField] private float radius = 3f;
    [SerializeField] private int damage = 3;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Animation Hookup")]
    [Tooltip("Animator directly on this prefab")]
    [SerializeField] private Animator animator;
    [Tooltip("Trigger parameter to play animation (leave empty to just play stateName).")]
    [SerializeField] private string playTrigger = "Play";
    [Tooltip("If no trigger is used, the state name to play on layer 0.")]
    [SerializeField] private string stateName = "Pulse";

    public bool Initialize(Vector2 dir, Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Uncommon: radius *= 1.1f; break;
            case Rarity.Rare: radius *= 1.3f; damage += 2; break;
            case Rarity.Epic: radius *= 1.5f; damage += 4; break;
            case Rarity.Legendary: radius *= 2f; damage += 7; break;
        }
        return true;
    }

    private void Start()
    {
        // Auto-size the visual to radius (assumes a circle sprite as base)
        FitCircleToRadius(gameObject, radius);

        // Play animation
        float animDuration = 0.35f;
        if (animator != null)
        {
            if (!string.IsNullOrEmpty(playTrigger))
            {
                animator.ResetTrigger(playTrigger);
                animator.SetTrigger(playTrigger);
            }
            else if (!string.IsNullOrEmpty(stateName))
            {
                animator.Play(stateName, 0, 0f);
            }

            animDuration = GetAnimatorStateLength(animator,
                string.IsNullOrEmpty(stateName) ? 0 : Animator.StringToHash(stateName));
            if (animDuration <= 0f) animDuration = 0.35f;
        }

        // Deal damage immediately (could also be triggered mid-anim by Animation Event)
        DoDamage();

        // Destroy when animation has finished
        Destroy(gameObject, animDuration + 0.05f);
    }

    private void DoDamage()
    {
        Collider2D[] hits = (enemyLayer.value == 0)
            ? Physics2D.OverlapCircleAll(transform.position, radius)
            : Physics2D.OverlapCircleAll(transform.position, radius, enemyLayer);

        var seen = new HashSet<Transform>();
        foreach (var col in hits)
        {
            if (!col) continue;

            Transform root = col.attachedRigidbody ? col.attachedRigidbody.transform : col.transform;
            if (!seen.Add(root)) continue;
            if (root.CompareTag("Player")) continue;

            var eh = root.GetComponent<EnemyHealth>();
            if (eh != null) eh.TakeDamage(damage);

            var bh = root.GetComponent<BossHealth>();
            if (bh != null) bh.TakeDamage(damage);
        }
    }

    private static void FitCircleToRadius(GameObject go, float targetRadius)
    {
        var sr = go.GetComponentInChildren<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            float currentDiameter = sr.bounds.size.x;
            if (currentDiameter > 0f)
            {
                float desiredDiameter = targetRadius * 2f;
                float scaleFactor = desiredDiameter / currentDiameter;
                go.transform.localScale *= scaleFactor;
                return;
            }
        }

        go.transform.localScale = Vector3.one * targetRadius * 2f;
    }

    private static float GetAnimatorStateLength(Animator animator, int expectedStateHash = 0)
    {
        var st = animator.GetCurrentAnimatorStateInfo(0);
        if (expectedStateHash == 0 || st.shortNameHash == expectedStateHash)
            return st.length;

        var rac = animator.runtimeAnimatorController;
        if (rac != null && rac.animationClips.Length > 0)
        {
            float max = 0f;
            foreach (var clip in rac.animationClips)
                if (clip.length > max) max = clip.length;
            return max;
        }

        return 0f;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
