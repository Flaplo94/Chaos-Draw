using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FireTrail : MonoBehaviour, IAbilityBehavior
{
    [Header("Trail Controller")]
    [SerializeField] private float duration = 3f;        // How long the trail ability runs on the player
    [SerializeField] private float dropInterval = 0.3f;  // Time between patch drops

    [Header("Patch (each drop)")]
    [SerializeField] private float patchLifetime = 2f;   // How long a single patch persists
    [SerializeField] private float radius = 2f;          // Damage radius per patch
    [SerializeField] private int damagePerTick = 1;      // Damage each tick
    [SerializeField] private float tickInterval = 0.5f;  // How fast the patch ticks
    [SerializeField] private LayerMask enemyLayer;       // 0 = no filter

    [Header("Visuals / Animation")]
    [Tooltip("Animator on this prefab (or child). Controller instance is hidden/disabled; patches enable and play 'Spawn'.")]
    [SerializeField] private Animator animator;          // Optional; auto-wired in Awake if missing
    [Tooltip("SpriteRenderer root for visuals to toggle visibility. Optional; auto-wired in Awake if missing.")]
    [SerializeField] private SpriteRenderer spriteRenderer; // Optional; auto-wired in Awake if missing
    [Tooltip("Trigger parameter on Animator to start the patch spawn animation. Leave empty to use 'Spawn' state by name.")]
    [SerializeField] private string spawnTrigger = "Spawn";
    [Tooltip("Fallback state name if you don't use a trigger.")]
    [SerializeField] private string spawnStateName = "Spawn";

    // --- Runtime ---
    private bool isController = true;           // true = spawner on player; false = a patch instance
    private Transform player;                   // controller uses this
    private Rarity rarityApplied = Rarity.Common;

    // Patch timers
    private float patchLifeTimer;
    private float tickTimer;

    // ---------------- IAbilityBehavior ----------------
    public bool Initialize(Vector2 _, Rarity rarity)
    {
        rarityApplied = rarity;

        // Optional rarity tuning
        switch (rarity)
        {
            case Rarity.Uncommon:
                duration *= 1.10f; radius *= 1.10f; break;
            case Rarity.Rare:
                duration *= 1.25f; radius *= 1.25f; damagePerTick += 1; break;
            case Rarity.Epic:
                duration *= 1.35f; radius *= 1.35f; damagePerTick += 2; break;
            case Rarity.Legendary:
                duration *= 1.50f; radius *= 1.50f; damagePerTick += 3; break;
                // Common = baseline
        }
        return true;
    }

    private void Awake()
    {
        // Auto-wire common components if not assigned
        if (!animator) animator = GetComponentInChildren<Animator>(true);
        if (!spriteRenderer) spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
    }

    private void Start()
    {
        if (isController)
        {
            // Make controller invisible and non-animating so it doesn't look like a long first patch
            if (animator) animator.enabled = false;
            if (spriteRenderer) spriteRenderer.enabled = false;

            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (!playerObj)
            {
                Debug.LogWarning("FireTrail: Player not found.");
                Destroy(gameObject);
                return;
            }

            player = playerObj.transform;
            StartCoroutine(LeaveTrail());
        }
        else
        {
            // Patch mode: show visuals and play spawn from a clean state
            if (spriteRenderer) spriteRenderer.enabled = true;

            if (animator)
            {
                animator.enabled = true;
                animator.Rebind();     // reset all states/params
                animator.Update(0f);   // evaluate first frame

                if (!string.IsNullOrEmpty(spawnTrigger))
                {
                    animator.ResetTrigger(spawnTrigger);
                    animator.SetTrigger(spawnTrigger);
                }
                else if (!string.IsNullOrEmpty(spawnStateName))
                {
                    animator.Play(spawnStateName, 0, 0f);
                }
            }
        }
    }

    private void Update()
    {
        if (isController) return;

        // Patch lifetime
        patchLifeTimer += Time.deltaTime;
        if (patchLifeTimer >= patchLifetime)
        {
            Destroy(gameObject);
            return;
        }

        // Patch tick damage
        tickTimer += Time.deltaTime;
        if (tickTimer >= tickInterval)
        {
            DoTickDamage();
            tickTimer = 0f;
        }
    }

    private IEnumerator LeaveTrail()
    {
        float timer = 0f;

        while (timer < duration)
        {
            if (player != null)
            {
                // Spawn a patch (clone of this object) in patch mode
                GameObject patch = Instantiate(gameObject, player.position, Quaternion.identity);

                // Configure the clone as a patch
                var fireTrail = patch.GetComponent<FireTrail>();
                fireTrail.isController = false;
                fireTrail.player = null;
                fireTrail.patchLifeTimer = 0f;
                fireTrail.tickTimer = 0f;

                // Ensure patch visuals/anim are enabled and restarted cleanly
                if (fireTrail.spriteRenderer) fireTrail.spriteRenderer.enabled = true;
                if (fireTrail.animator)
                {
                    fireTrail.animator.enabled = true;
                    fireTrail.animator.Rebind();
                    fireTrail.animator.Update(0f);

                    if (!string.IsNullOrEmpty(spawnTrigger))
                    {
                        fireTrail.animator.ResetTrigger(spawnTrigger);
                        fireTrail.animator.SetTrigger(spawnTrigger);
                    }
                    else if (!string.IsNullOrEmpty(spawnStateName))
                    {
                        fireTrail.animator.Play(spawnStateName, 0, 0f);
                    }
                }
            }

            yield return new WaitForSeconds(dropInterval);
            timer += dropInterval;
        }

        Destroy(gameObject); // controller ends
    }

    private void DoTickDamage()
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

            if (root.TryGetComponent(out EnemyHealth eh)) eh.TakeDamage(damagePerTick);
            if (root.TryGetComponent(out BossHealth bh)) bh.TakeDamage(damagePerTick);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
#endif
}
