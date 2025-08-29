using UnityEngine;

public class EnemyAnimator : MonoBehaviour
{
    private Animator animator;
    private Transform enemyRoot;

    [Header("States (must match Animator)")]
    [SerializeField] private string walkStateName = "WalkBT";
    [SerializeField] private string attackStateName = "AttackBT";
    [SerializeField] private string dieStateName = "DieBT";

    [Header("Params")]
    [SerializeField] private float movingThreshold = 0.01f;

    private Rigidbody2D rb;
    private Transform player;
    private Vector2 lastDir = Vector2.down;

    void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (enemyRoot == null) enemyRoot = transform.root;

        rb = GetComponent<Rigidbody2D>();

        var pgo = GameObject.FindWithTag("Player");
        if (pgo != null) player = pgo.transform;
    }

    void Update()
    {
        if (!Ready()) return;

        float speed = rb != null ? rb.linearVelocity.magnitude : 0f;
        bool isMoving = speed > movingThreshold;
        animator.SetBool("IsMoving", isMoving);

        Vector2 dir;
        if (isMoving && rb != null)
            dir = rb.linearVelocity;
        else if (player != null && enemyRoot != null)
            dir = (Vector2)(player.position - enemyRoot.position);
        else
            dir = lastDir;

        Vector2 snapped = SnapToCardinal(dir);
        lastDir = snapped;

        animator.SetFloat("MoveX", snapped.x);
        animator.SetFloat("MoveY", snapped.y);
    }

    public void PlayAttack()
    {
        if (!Ready()) return;
        animator.ResetTrigger("Attack");
        animator.SetTrigger("Attack");
    }

    public void PlayDie()
    {
        if (!Ready()) return;
        animator.ResetTrigger("Die");
        animator.SetTrigger("Die");
    }

    private bool Ready()
    {
        return animator != null && animator.isActiveAndEnabled && animator.runtimeAnimatorController != null;
    }

    private Vector2 SnapToCardinal(Vector2 v)
    {
        if (v.sqrMagnitude < 0.0001f) return Vector2.down;
        if (Mathf.Abs(v.x) > Mathf.Abs(v.y)) return new Vector2(Mathf.Sign(v.x), 0f);
        else return new Vector2(0f, Mathf.Sign(v.y));
    }
}
