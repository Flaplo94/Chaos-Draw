using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public class StunReceiver : MonoBehaviour
{
    [Header("Stun Settings")]
    [Tooltip("If off, this unit will ignore stun attempts.")]
    public bool canBeStunned = true;

    public bool IsCurrentlyStunned { get; private set; }

    private float timer;
    private readonly List<Behaviour> disabledBehaviours = new List<Behaviour>();
    private Animator anim;
    private float prevAnimSpeed = 1f;
    private Rigidbody2D rb;
    private Vector2 savedVel;

    void Awake()
    {
        anim = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (!IsCurrentlyStunned) return;
        timer -= Time.deltaTime;
        if (timer <= 0f) ExitStun();
    }

    public void ApplyStun(float duration)
    {
        if (!canBeStunned || duration <= 0f) return;

        if (!IsCurrentlyStunned) EnterStun();
        // If already stunned, extend the timer to the longest remaining
        timer = Mathf.Max(timer, duration);
    }

    private void EnterStun()
    {
        IsCurrentlyStunned = true;

        // Freeze physics (if present)
        if (rb != null)
        {
            savedVel = rb.linearVelocity;
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        // Pause animation (if present)
        if (anim != null)
        {
            prevAnimSpeed = anim.speed;
            anim.speed = 0f;
        }

        // Disable non-essential scripts on THIS object to stop AI/attacks/movement
        foreach (var b in GetComponents<Behaviour>())
        {
            if (b == null || !b.enabled) continue;
            if (IsWhitelisted(b)) continue;
            b.enabled = false;
            disabledBehaviours.Add(b);
        }
    }

    private void ExitStun()
    {
        // Re-enable scripts
        for (int i = 0; i < disabledBehaviours.Count; i++)
        {
            if (disabledBehaviours[i]) disabledBehaviours[i].enabled = true;
        }
        disabledBehaviours.Clear();

        // Restore animation & physics
        if (anim != null) anim.speed = prevAnimSpeed;
        if (rb != null)
        {
            rb.simulated = true;
            rb.linearVelocity = savedVel;
        }

        IsCurrentlyStunned = false;
        timer = 0f;
    }

    private bool IsWhitelisted(Behaviour b)
    {
        // Keep core components running
        var t = b.GetType();
        if (b is StunReceiver) return true;
        if (t.Name.Contains("Health")) return true;              // EnemyHealth / BossHealth, etc.
        if (b is Animator) return true;                           // we pause its speed instead
        if (b is SpriteRenderer) return true;
        if (b is SortingGroup) return true;
        if (b is Collider2D) return true;
        if (b is Rigidbody2D) return true;
        return false;
    }

    // Handy static helper for one-liners:
    public static void ApplyTo(Transform targetRoot, float duration)
    {
        if (!targetRoot) return;
        var s = targetRoot.GetComponent<StunReceiver>();
        if (!s) s = targetRoot.gameObject.AddComponent<StunReceiver>();
        s.ApplyStun(duration);
    }

    public static bool IsStunned(GameObject go)
    {
        if (go == null) return false;
        StunReceiver sr;
        if (go.TryGetComponent(out sr))
            return sr != null && sr.enabled && sr.IsCurrentlyStunned;
        return false;
    }
}
