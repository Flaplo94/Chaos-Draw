using UnityEngine;
using System.Collections.Generic;

[DefaultExecutionOrder(1000)] // run late in FixedUpdate relative to typical AI
public class TimeBubbleEffector : MonoBehaviour
{
    // sources -> multiplier (0..1)
    private readonly Dictionary<Object, float> _sources = new Dictionary<Object, float>();

    private Rigidbody2D rb;
    private Vector2 lastRbPos;
    private bool hadLastRbPos;

    private Vector3 lastTfPos;
    private bool hadLastTfPos;

    private Bullet cachedBullet;
    private bool isDynamicBullet;
    private void Awake()
    {
        // If this effector ever ends up on a bullet, nuke it immediately.
        var bullet = GetComponent<Bullet>();
        if (bullet != null)
        {
            Destroy(this);
            return;
        }

        rb = GetComponent<Rigidbody2D>();
    }

    public void AddSource(Object owner, float multiplier)
    {
        if (!owner) return;
        _sources[owner] = Mathf.Clamp01(multiplier);
        cachedBullet = GetComponent<Bullet>();
        if (cachedBullet)
        {
            rb = cachedBullet.GetComponent<Rigidbody2D>();
            if (rb && rb.bodyType == RigidbodyType2D.Dynamic)
                isDynamicBullet = true;
        }
        if (rb != null)
        {
            if (!hadLastRbPos) { lastRbPos = rb.position; hadLastRbPos = true; }
        }
        else
        {
            if (!hadLastTfPos) { lastTfPos = transform.position; hadLastTfPos = true; }
        }

        enabled = true;
    }

    public void RemoveSource(Object owner)
    {
        if (!owner) return;
        _sources.Remove(owner);
        if (_sources.Count == 0) enabled = false;
    }

    private float M()
    {
        float m = 1f;
        foreach (var kv in _sources) m *= kv.Value;
        return Mathf.Clamp01(m);
    }

    private void OnDisable()
    {
        _sources.Clear();
        hadLastRbPos = false;
        hadLastTfPos = false;
    }

    private void FixedUpdate()
    {
        if (_sources.Count == 0) return;
        if (rb == null || !rb.simulated) return;

        float m = M();

        //  For dynamic bullets (your projectiles): only scale velocity
        if (isDynamicBullet)
        {
            rb.linearVelocity *= m;
            return;
        }

        // For everything else (enemies, enemy projectiles, kinematic movers)
        if (rb.bodyType == RigidbodyType2D.Dynamic)
        {
            rb.linearVelocity *= m;
            lastRbPos = rb.position;
            hadLastRbPos = true;
        }
        else
        {
            if (!hadLastRbPos)
            {
                lastRbPos = rb.position;
                hadLastRbPos = true;
                return;
            }

            Vector2 current = rb.position;
            Vector2 step = current - lastRbPos;
            if (step.sqrMagnitude > 0f)
            {
                Vector2 reduced = step * m;
                Vector2 target = lastRbPos + reduced;
                rb.MovePosition(target);
                lastRbPos = target;
            }
            else
            {
                lastRbPos = current;
            }
        }
    }


    private void LateUpdate()
    {
        if (_sources.Count == 0) return;

        // If we have an RB, we manage slowdown in FixedUpdate only.
        if (rb != null && rb.simulated) return;

        float m = M();

        if (!hadLastTfPos)
        {
            lastTfPos = transform.position;
            hadLastTfPos = true;
            return;
        }

        var current = transform.position;
        var delta = current - lastTfPos;

        if (delta.sqrMagnitude > 0f)
        {
            var reduced = delta * m;
            transform.position = lastTfPos + reduced;
        }

        lastTfPos = transform.position;
    }
}
