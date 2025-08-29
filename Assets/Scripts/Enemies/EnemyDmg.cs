using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider2D))]
public class EnemyDmg : MonoBehaviour
{
    [Header("Damage / Range / Cooldown")]
    [SerializeField] public int damageAmount = 1;
    [SerializeField] public float attackCooldown = 1.0f;
    [SerializeField] public float attackRange = 1.0f;

    
    private EnemyAnimator enemyAnimator;
    private Transform enemyRoot;

    private Collider2D hitbox;
    private Transform player;
    private float lastAttackTime = -999f;
    private bool isAttacking = false;
    private bool hitboxActive = false;
    private HashSet<PlayerHealth> hitThisSwing = new HashSet<PlayerHealth>();

    void Awake()
    {
        hitbox = GetComponent<Collider2D>();
        //if (hitbox != null) { hitbox.isTrigger = true; hitbox.enabled = false; }

        if (enemyAnimator == null)
            enemyAnimator = GetComponentInParent<EnemyAnimator>();

        if (enemyRoot == null)
        {
            enemyRoot = transform.root;
            var follow = GetComponentInParent<EnemyFollow>();
            enemyRoot = follow != null ? follow.transform : transform.root;
        }

        var pgo = GameObject.FindWithTag("Player");
        if (pgo != null) player = pgo.transform;
    }

    void Update()
    {
        if (player == null) return;

        float dist = Vector2.Distance(enemyRoot.position, player.position);

        if (!isAttacking && dist <= attackRange && (Time.time - lastAttackTime) >= attackCooldown)
        {
            StartAttack();
        }
    }

    private void StartAttack()
    {
        isAttacking = true;
        hitThisSwing.Clear();
        enemyAnimator.PlayAttack();
    }

    // Animation Events
    public void AE_HitboxOn()
    {
        hitboxActive = true;
        if (hitbox != null) hitbox.enabled = true;
    }

    public void AE_HitboxOff()
    {
        hitboxActive = false;
        if (hitbox != null) hitbox.enabled = false;
    }

    public void AE_AttackFinished()
    {
        isAttacking = false;
        lastAttackTime = Time.time;
        hitboxActive = false;
        if (hitbox != null) hitbox.enabled = false;
        hitThisSwing.Clear();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!hitboxActive) return;
        if (!other.CompareTag("Player")) return;

        var ph = other.GetComponentInParent<PlayerHealth>();
                 
        if (ph == null) return;

        if (hitThisSwing.Contains(ph)) return;

        ph.TakeDamage(damageAmount);
        hitThisSwing.Add(ph);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        // TEMPORARY: simple touch damage with cooldown
        if (!other.CompareTag("Player")) return;

        var ph = other.GetComponentInParent<PlayerHealth>();
        if (ph == null) return;

        if ((Time.time - lastAttackTime) >= attackCooldown)
        {
            ph.TakeDamage(damageAmount);
            lastAttackTime = Time.time;
            Debug.Log($"{name} TEMP damage tick to {ph.name}");
        }
    }

}
