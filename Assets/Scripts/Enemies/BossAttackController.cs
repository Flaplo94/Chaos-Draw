using System.Collections;
using UnityEngine;

public class BossAttackController : MonoBehaviour
{
    [Header("General")]
    [SerializeField] private Transform player;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private float thinkInterval = 0.15f;

    [Header("Melee (basic)")]
    [SerializeField] private float meleeRange = 1.8f;
    [SerializeField] private int meleeDamage = 10;
    [SerializeField] private float meleeCooldown = 1.2f;
    [SerializeField] private Transform meleeOrigin;
    [SerializeField] private float meleeHitboxRadius = 1f;
    [SerializeField] private GameObject meleePrefab;
    [SerializeField] private string meleeTriggerName = "Melee";
    [SerializeField] private float meleeVisualOffset = 1.0f;

    [Header("Black Hole")]
    [SerializeField] private GameObject blackHolePrefab;
    [SerializeField] private float blackHoleMinSpawnDistanceFromPlayer = 1.5f;
    [SerializeField] private float blackHoleMaxSpawnDistanceFromPlayer = 3.5f;
    [SerializeField] private float blackHoleCooldown = 6f;
    [SerializeField] private Animator blackHoleAnimator;
    [SerializeField] private string blackHoleTriggerName = "BlackHole";
    [SerializeField] private int blackHoleSpawnAttempts = 6;
    [SerializeField] private float blackHoleAvoidPlayerRadius = 0.6f;

    [Header("Crowd Control")]
    [SerializeField] private GameObject ccPrefab;                 // must have a Trigger collider
    [SerializeField] private GameObject ccIndicatorPrefab;        // visual telegraph prefab (optional Animator/FX)
    [SerializeField] private float ccWindupTime = 0.75f;          // how long the indicator shows before the hitbox
    [SerializeField] private float ccRootDuration = 2f;           // root duration to apply to player
    [SerializeField] private float ccCooldown = 5f;
    [SerializeField] private Animator ccAnimator;
    [SerializeField] private string ccTriggerName = "CrowdControl";

    [Header("Strike From Above")]
    [SerializeField] private GameObject strikePrefab;
    [SerializeField] private float strikeCooldown = 4.5f;
    [SerializeField] private Animator strikeAnimator;
    [SerializeField] private string strikeTriggerName = "Strike";

    [Header("Global Attack Timing")]
    [SerializeField] private float spawnDelay = 1.25f;      // time after spawn before ANY attack can happen
    [SerializeField] private float interAttackDelay = 1.25f; // minimum gap between finishing one attack and starting the next

    private float nextActionTime; // timestamp when the AI is allowed to pick the next attack


    private bool meleeReady = true;
    private bool blackHoleReady = true;
    private bool ccReady = true;
    private bool strikeReady = true;

    private void Awake()
    {
        if (player == null)
        {
            var marker = FindFirstObjectByType<PlayerMarker>();
            if (marker != null) player = marker.transform;
            if (player == null)
            {
                var ph = FindFirstObjectByType<PlayerHealth>();
                if (ph != null) player = ph.transform;
            }
        }
    }

    private void OnEnable()
    {
        nextActionTime = Time.time + spawnDelay;
        StartCoroutine(BrainLoop());
    }

    private IEnumerator BrainLoop()
    {
        var wait = new WaitForSeconds(thinkInterval);
        while (true)
        {
            if (player != null)
            {
                // do nothing until the global timer allows the next action
                if (Time.time >= nextActionTime)
                {
                    float dist = Vector2.Distance(transform.position, player.position);

                    // pick exactly ONE action, then push nextActionTime forward
                    if (dist <= meleeRange && meleeReady)
                    {
                        StartCoroutine(DoMelee());
                    }
                    else if (blackHoleReady)
                    {
                        nextActionTime = Time.time + interAttackDelay;
                        StartCoroutine(DoBlackHole());
                    }
                    else if (strikeReady)
                    {
                        nextActionTime = Time.time + interAttackDelay;
                        StartCoroutine(DoStrike());
                    }
                    else if (ccReady)
                    {
                        nextActionTime = Time.time + interAttackDelay;
                        StartCoroutine(DoCrowdControl());
                    }
                }
            }
            yield return wait;
        }
    }

    private IEnumerator DoMelee()
    {
        meleeReady = false;

        // spawn melee visual prefab at offset toward player
        if (meleePrefab && meleeOrigin && player)
        {
            Vector2 dir = ((Vector2)player.position - (Vector2)meleeOrigin.position).normalized;
            Vector3 spawnPos = meleeOrigin.position + (Vector3)(dir * meleeVisualOffset);

            Quaternion rot = Quaternion.FromToRotation(Vector3.right, new Vector3(dir.x, dir.y, 0f));
            var vis = Instantiate(meleePrefab, spawnPos, rot);

            var mv = vis.GetComponent<MeleeVisual>();
            if (mv != null)
            {
                mv.Setup(dir);
            }
        }

        // tiny windup
        yield return new WaitForSeconds(0.1f);

        // DAMAGE from the boss center (not from the visual)
        Vector2 center = meleeOrigin ? (Vector2)meleeOrigin.position : (Vector2)transform.position;
        Collider2D hit = Physics2D.OverlapCircle(center, meleeHitboxRadius, playerLayer);
        if (hit)
        {
            var health = hit.GetComponentInParent<PlayerHealth>();
            if (health) health.TakeDamage(meleeDamage);
        }

        yield return new WaitForSeconds(meleeCooldown);
        meleeReady = true;
    }




    private IEnumerator DoBlackHole()
    {
        blackHoleReady = false;

        if (blackHoleAnimator && !string.IsNullOrEmpty(blackHoleTriggerName))
            blackHoleAnimator.SetTrigger(blackHoleTriggerName);

        if (blackHolePrefab && player)
        {
            float minD = Mathf.Max(0.1f, Mathf.Min(blackHoleMinSpawnDistanceFromPlayer, blackHoleMaxSpawnDistanceFromPlayer));
            float maxD = Mathf.Max(blackHoleMinSpawnDistanceFromPlayer, blackHoleMaxSpawnDistanceFromPlayer);

            Vector2 chosenPos = (Vector2)player.position;

            for (int i = 0; i < Mathf.Max(1, blackHoleSpawnAttempts); i++)
            {
                float angleRad = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float dist = Random.Range(minD, maxD);
                Vector2 offset = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad)) * dist;
                Vector2 candidate = (Vector2)player.position + offset;

                Collider2D onPlayer = Physics2D.OverlapCircle(candidate, blackHoleAvoidPlayerRadius, playerLayer);
                if (onPlayer == null)
                {
                    chosenPos = candidate;
                    break;
                }
            }

            Instantiate(blackHolePrefab, chosenPos, Quaternion.identity);
        }

        yield return new WaitForSeconds(blackHoleCooldown);
        blackHoleReady = true;
    }

    private IEnumerator DoCrowdControl()
    {
        ccReady = false;

        if (ccAnimator && !string.IsNullOrEmpty(ccTriggerName))
            ccAnimator.SetTrigger(ccTriggerName);

        if (player)
        {
            // lock the target position at windup start (indicator shows here)
            Vector2 targetPos = player.position;

            // spawn telegraph/indicator (purely visual)
            if (ccIndicatorPrefab)
            {
                var indicator = Instantiate(ccIndicatorPrefab, targetPos, Quaternion.identity);
                Destroy(indicator, ccWindupTime + 0.1f); // clean up after wind-up
            }

            // wait wind-up, then spawn the actual CC hitbox
            yield return new WaitForSeconds(ccWindupTime);

            if (ccPrefab)
            {
                var go = Instantiate(ccPrefab, targetPos, Quaternion.identity);

                // if the hitbox script supports setup, pass in layer+duration so it can actually root
                var touch = go.GetComponent<CCOnTouch>();
                if (touch != null)
                {
                    touch.Setup(playerLayer, ccRootDuration);
                }
            }
        }

        // cooldown
        yield return new WaitForSeconds(ccCooldown);
        ccReady = true;
    }

    private IEnumerator DoStrike()
    {
        strikeReady = false;
        if (strikeAnimator && !string.IsNullOrEmpty(strikeTriggerName)) strikeAnimator.SetTrigger(strikeTriggerName);

        if (strikePrefab)
        {
            Vector2 targetPos = player ? (Vector2)player.position : (Vector2)transform.position;
            Instantiate(strikePrefab, targetPos, Quaternion.identity);
        }

        yield return new WaitForSeconds(strikeCooldown);
        strikeReady = true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        if (meleeOrigin) Gizmos.DrawWireSphere(meleeOrigin.position, meleeHitboxRadius);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, meleeRange);
    }
}
