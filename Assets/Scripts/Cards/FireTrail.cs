using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FireTrail : MonoBehaviour, IAbilityBehavior
{
    [Header("Trail Controller")]
    [SerializeField] private float duration = 3f;
    [SerializeField] private float dropInterval = 0.3f;
    [SerializeField] private float range = 0.5f;

    [Header("Patch (each drop)")]
    [SerializeField] private float patchLifetime = 2f;
    [SerializeField] private float radius = 2f;
    [SerializeField] private int damagePerTick = 1;
    [SerializeField] private float tickInterval = 0.5f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Visuals / Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private string spawnTrigger = "Spawn";
    [SerializeField] private string spawnStateName = "Spawn";

    [Header("Lucky Shot (side-by-side)")]
    [Tooltip("Sideways spacing between the two trails when Lucky Shot procs.")]
    [SerializeField] private float duplicateSideDistance = 1.2f;

    // Controller / state
    private bool isController = true;
    private Transform player;
    private Rarity rarityApplied = Rarity.Common;

    private float patchLifeTimer;
    private float tickTimer;

    // Lucky Shot bookkeeping
    private bool luckyWasDuplicated = false;       // prevents re-duplication
    private Vector2 castDir = Vector2.right;       // aim captured on Initialize
    private Vector2 trailOffsetWorld = Vector2.zero; // this controller's lateral offset for patches

    public bool Initialize(Vector2 dir, Rarity rarity)
    {
        rarityApplied = rarity;

        // capture aim for left/right split; default to world-right
        castDir = (dir.sqrMagnitude > 0.0001f) ? dir.normalized : Vector2.right;

        // your original rarity scaling
        switch (rarity)
        {
            case Rarity.Uncommon: duration *= 1.10f; radius *= 1.10f; damagePerTick += 1; break;
            case Rarity.Rare: duration *= 1.25f; radius *= 1.25f; damagePerTick += 2; break;
            case Rarity.Epic: duration *= 1.35f; radius *= 1.35f; damagePerTick += 3; break;
            case Rarity.Legendary: duration *= 1.50f; radius *= 1.50f; damagePerTick += 4; break;
        }
        return true;
    }

    private void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>(true);
        if (!spriteRenderer) spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
    }

    private void Start()
    {
        if (isController)
        {
            // Controller is invisible; it just drops patches.
            if (animator) animator.enabled = false;
            if (spriteRenderer) spriteRenderer.enabled = false;

            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (!playerObj) { Destroy(gameObject); return; }
            player = playerObj.transform;

            // If Lucky Shot procs this cast  split into two controllers (left/right) and start both.
            if (!luckyWasDuplicated)
            {
                bool spawnedDouble = false;

                LuckyShotSystem.OnSpellCast(() =>
                {
                    spawnedDouble = true;

                    // Perpendicular to aim (left/right)
                    Vector2 dir = (castDir.sqrMagnitude > 0.0001f) ? castDir : Vector2.right;
                    Vector2 side = new Vector2(-dir.y, dir.x).normalized;

                    // Make the duplicate controller on +side
                    var dup = Instantiate(gameObject, transform.position, transform.rotation);
                    var comp = dup.GetComponent<FireTrail>();
                    if (comp != null)
                    {
                        comp.luckyWasDuplicated = true;      // no further split
                        comp.isController = true;
                        comp.player = this.player;
                        comp.rarityApplied = this.rarityApplied;
                        comp.castDir = this.castDir;
                        comp.trailOffsetWorld = side * (duplicateSideDistance * 0.5f);
                        comp.StartCoroutine(comp.LeaveTrail()); // start the duplicate controller
                    }

                    // Move THIS controller to the -side and start it
                    this.trailOffsetWorld = -side * (duplicateSideDistance * 0.5f);
                    StartCoroutine(LeaveTrail());
                });

                // If Lucky Shot did NOT proc (regular cast)  single centered trail.
                if (!spawnedDouble)
                {
                    trailOffsetWorld = Vector2.zero;
                    StartCoroutine(LeaveTrail());
                }

                return;
            }

            // If this is the duplicate controller (flagged), just run normally
            StartCoroutine(LeaveTrail());
        }
        else
        {
            // Patch instance visuals
            if (spriteRenderer) spriteRenderer.enabled = true;

            if (animator)
            {
                animator.enabled = true;
                animator.Rebind();
                animator.Update(0f);

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

        patchLifeTimer += Time.deltaTime;
        if (patchLifeTimer >= patchLifetime) { Destroy(gameObject); return; }

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
                // spawn patch at player's current pos + this controller's side offset
                Vector3 patchPos = player.position + (Vector3)trailOffsetWorld;

                GameObject patch = Instantiate(gameObject, patchPos, Quaternion.identity);
                var fireTrail = patch.GetComponent<FireTrail>();
                fireTrail.isController = false;
                fireTrail.player = null;
                fireTrail.patchLifeTimer = 0f;
                fireTrail.tickTimer = 0f;

                // Patches don't need trail offset; they are static on spawn position.
                fireTrail.trailOffsetWorld = Vector2.zero;
                fireTrail.castDir = this.castDir;

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

        Destroy(gameObject); // controller ends after duration
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

            var result = DamageCalculator.ComputeFinalDamage(damagePerTick, DamageElement.Fire);
            if (root.TryGetComponent(out EnemyHealth eh))
            {
                eh.TakeDamage(result.amount, result.element);
                BurnRules.TryApplyBurn(root, result.amount);
            }
            if (root.TryGetComponent(out BossHealth bh))
            {
                bh.TakeDamage(result.amount, result.element);
                BurnRules.TryApplyBurn(root, result.amount);
            }
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
