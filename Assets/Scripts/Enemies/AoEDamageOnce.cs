using System.Collections;
using UnityEngine;

public class AoEDamageOnce : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private int damage = 15;
    [SerializeField] private LayerMask playerLayer;

    [Header("Timing")]
    [Tooltip("How long the collider stays active after being triggered by the animation event.")]
    [SerializeField] private float activeTime = 0.08f;

    [Header("Strike Size")]
    [Tooltip("Scales both the visual (sprite/anim) and the collider at spawn.")]
    [SerializeField] private float strikeScale = 1f;

    private Collider2D col2D;
    private bool fired;
    private bool hasDealtDamage;

    private void Awake()
    {
        col2D = GetComponent<Collider2D>();
        if (col2D != null) col2D.enabled = false;

        // scale the whole GameObject so visuals + collider both grow
        if (strikeScale != 1f)
        {
            transform.localScale *= strikeScale;
        }
    }

    /// <summary>
    /// Call this from an Animation Event at the frame when the attack should become active.
    /// Example: put event at frame 3 in your strike animation.
    /// </summary>
    public void ActivateFromAnimation()
    {
        if (!fired)
        {
            StartCoroutine(FireRoutine());
        }
    }

    private IEnumerator FireRoutine()
    {
        fired = true;

        // turn collider on
        if (col2D != null)
        {
            col2D.enabled = true;
            TryDamageOverlappingOnce();
        }

        // keep active for set time
        yield return new WaitForSeconds(activeTime);

        // disable collider again
        if (col2D != null) col2D.enabled = false;
    }

    private void TryDamageOverlappingOnce()
    {
        if (hasDealtDamage || col2D == null) return;

        var filter = new ContactFilter2D();
        filter.useTriggers = true;
        filter.SetLayerMask(playerLayer);

        Collider2D[] results = new Collider2D[4];
        int count = col2D.Overlap(filter, results);   // Unity 6 Overlap API

        if (count > 0)
        {
            for (int i = 0; i < count; i++)
            {
                if (results[i] == null) continue;

                var health = results[i].GetComponentInParent<PlayerHealth>();
                if (health != null)
                {
                    health.TakeDamage(damage);
                    hasDealtDamage = true;
                    break;
                }
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasDealtDamage) return;
        if (((1 << other.gameObject.layer) & playerLayer) == 0) return;

        var health = other.GetComponentInParent<PlayerHealth>();
        if (health != null)
        {
            health.TakeDamage(damage);
            hasDealtDamage = true;
        }
    }
    public void EndFromAnimation()
    {
        Destroy(gameObject);
    }
}
