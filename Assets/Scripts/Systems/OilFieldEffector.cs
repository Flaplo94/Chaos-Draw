using UnityEngine;
using System.Reflection;

/// <summary>
/// Per-enemy token that applies a temporary slow, non-compounding.
/// </summary>
[DisallowMultipleComponent]
public class OilFieldEffector : MonoBehaviour
{
    private Rigidbody2D rb;

    // Active slow
    private float activeMult = 1f;       // 1 = no slow; 0.40 = 60% slow
    private float timeLeft = 0f;

    // Non-compounding bookkeeping
    [SerializeField] private float minMult = 0.15f; // floor to avoid hard freeze
    private float lastAppliedMult = 1f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    /// <summary>Apply or refresh a slow (mult  (0..1], duration in seconds).</summary>
    public void Refresh(float mult, float duration)
    {
        activeMult = Mathf.Clamp(mult, minMult, 1f);
        timeLeft = Mathf.Max(timeLeft, duration); // refresh to the longest
        // Optional: also try to inform common enemy movement APIs if present
        TryApplyViaCommonAPIs(activeMult);
    }

    private void FixedUpdate()
    {
        // countdown
        if (timeLeft > 0f)
        {
            timeLeft -= Time.fixedDeltaTime;
        }

        float targetMult = (timeLeft > 0f) ? activeMult : 1f;

        // Non-compounding scaling of current velocity
        if (rb && !Mathf.Approximately(targetMult, lastAppliedMult))
        {
            float safeLast = (lastAppliedMult <= 0.0001f) ? 1f : lastAppliedMult;
            float factor = targetMult / safeLast;
            rb.linearVelocity *= factor;
            lastAppliedMult = targetMult;
        }

        // Auto-destroy when effect expired and we've restored to 1x
        if (timeLeft <= 0f && Mathf.Approximately(lastAppliedMult, 1f))
        {
            Destroy(this);
        }
    }

    /// <summary>
    /// OPTIONAL: if your enemies expose a speed API (e.g., EnemyFollow.SetExternalSpeedMult(float)),
    /// call it here so AI-driven velocity respects the slow even if it rewrites velocity each frame.
    /// This is non-destructive and only fires if such methods exist.
    /// </summary>
    private void TryApplyViaCommonAPIs(float mult)
    {
        var t = GetType().DeclaringType; // not used; just keeping reflection local to this object’s comps
        var comps = GetComponents<MonoBehaviour>();
        foreach (var c in comps)
        {
            if (!c) continue;
            var ct = c.GetType();

            // Look for a method that sets/updates an external speed multiplier
            var m = ct.GetMethod("SetExternalSpeedMultiplier", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (m != null && m.GetParameters().Length == 1)
            {
                try { m.Invoke(c, new object[] { mult }); return; } catch { }
            }

            m = ct.GetMethod("ApplySlowMultiplier", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (m != null && m.GetParameters().Length == 1)
            {
                try { m.Invoke(c, new object[] { mult }); return; } catch { }
            }
        }
    }
}
