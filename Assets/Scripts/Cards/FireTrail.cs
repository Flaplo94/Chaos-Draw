using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FireTrail : MonoBehaviour, IAbilityBehavior
{
    [Header("Trail Controller")]
    [SerializeField] private float duration = 3f;
    [SerializeField] private float dropInterval = 0.3f;

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

    private bool isController = true;
    private Transform player;
    private Rarity rarityApplied = Rarity.Common;

    private float patchLifeTimer;
    private float tickTimer;

    public bool Initialize(Vector2 _, Rarity rarity)
    {
        rarityApplied = rarity;
        switch (rarity)
        {
            case Rarity.Uncommon: duration *= 1.10f; radius *= 1.10f; break;
            case Rarity.Rare: duration *= 1.25f; radius *= 1.25f; damagePerTick += 1; break;
            case Rarity.Epic: duration *= 1.35f; radius *= 1.35f; damagePerTick += 2; break;
            case Rarity.Legendary: duration *= 1.50f; radius *= 1.50f; damagePerTick += 3; break;
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
            if (animator) animator.enabled = false;
            if (spriteRenderer) spriteRenderer.enabled = false;

            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (!playerObj)
            {
                Destroy(gameObject);
                return;
            }

            player = playerObj.transform;
            StartCoroutine(LeaveTrail());
        }
        else
        {
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
        if (patchLifeTimer >= patchLifetime)
        {
            Destroy(gameObject);
            return;
        }

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
                GameObject patch = Instantiate(gameObject, player.position, Quaternion.identity);
                var fireTrail = patch.GetComponent<FireTrail>();
                fireTrail.isController = false;
                fireTrail.player = null;
                fireTrail.patchLifeTimer = 0f;
                fireTrail.tickTimer = 0f;

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

        Destroy(gameObject);
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

            if (root.TryGetComponent(out EnemyHealth eh)) eh.TakeDamage(damagePerTick, DamageElement.Burn);
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
