using UnityEngine;
using System.Collections.Generic;

public class PlaceWall : MonoBehaviour, IAbilityBehavior
{
    [Header("Base Settings")]
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private Collider2D wallCollider;
    [SerializeField] private GameObject visual;

    [Header("Thorns (enabled from Rare+)")]
    [SerializeField] private bool thornsEnabled = false;
    [SerializeField] private int thornsDamage = 1;
    [SerializeField] private float thornsInterval = 0.5f; // seconds between ticks per enemy

    // per-enemy tick cooldown
    private readonly Dictionary<Collider2D, float> nextTickTime = new Dictionary<Collider2D, float>();

    // Called by Ability after Instantiate
    public void Initialize(Vector2 _, Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Uncommon:
                lifetime *= 1.10f;
                break;

            case Rarity.Rare:
                lifetime *= 1.20f;
                thornsEnabled = true;
                thornsDamage = Mathf.Max(thornsDamage, 1);
                // interval unchanged at Rare
                break;

            case Rarity.Epic:
                lifetime *= 1.30f;
                thornsEnabled = true;
                thornsDamage = Mathf.Max(thornsDamage, 2);
                thornsInterval *= 0.9f;   // ticks a bit faster
                break;

            case Rarity.Legendary:
                lifetime *= 1.40f;
                thornsEnabled = true;
                thornsDamage = Mathf.Max(thornsDamage, 3);
                thornsInterval *= 0.8f;   // ticks even faster
                break;

                // Common: baseline
        }

        // safety
        if (thornsInterval < 0.05f) thornsInterval = 0.05f;
        if (lifetime < 0.1f) lifetime = 0.1f;
    }

    private void Start()
    {
        if (wallCollider != null) wallCollider.enabled = true;
        if (visual != null) visual.SetActive(true);

        Invoke(nameof(DestroySelf), lifetime);
    }

    private void DestroySelf()
    {
        Destroy(gameObject);
    }

    // If wall uses solid collider (IsTrigger = false)
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!thornsEnabled) return;
        TryTickThorns(collision.collider);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        nextTickTime.Remove(collision.collider);
    }

    // If wall uses trigger collider (IsTrigger = true)
    private void OnTriggerStay2D(Collider2D other)
    {
        if (!thornsEnabled) return;
        TryTickThorns(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        nextTickTime.Remove(other);
    }

    private void TryTickThorns(Collider2D target)
    {
        // Damage enemies (and bosses, if present)
        bool isEnemy = target.TryGetComponent(out EnemyHealth enemy);
        bool isBoss = target.TryGetComponent(out BossHealth boss);

        if (!isEnemy && !isBoss) return;

        float now = Time.time;
        if (!nextTickTime.TryGetValue(target, out float next))
        {
            next = 0f;
        }

        if (now >= next)
        {
            if (isEnemy) enemy.TakeDamage(thornsDamage);
            if (isBoss) boss.TakeDamage(thornsDamage);

            nextTickTime[target] = now + thornsInterval;
        }
    }
}
