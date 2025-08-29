using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerAnimator : MonoBehaviour
{
    private Animator animator;
    private PlayerMovement movement;
    private PlayerHealth health;

    private Vector2 lastDir = Vector2.down; // default facing down

    void Awake()
    {
        animator = GetComponent<Animator>();
        movement = GetComponent<PlayerMovement>();
        health = GetComponent<PlayerHealth>();
    }

    void Update()
    {
        Vector2 dir = movement.MoveDir;

        if (dir.sqrMagnitude > 0.01f)
        {
            lastDir = dir; // remember last movement direction
        }

        // Walk Blend Tree params
        animator.SetFloat("MoveX", dir.x);
        animator.SetFloat("MoveY", dir.y);
        animator.SetBool("IsMoving", dir.sqrMagnitude > 0.01f);

        // Idle Blend Tree params
        animator.SetFloat("LastX", lastDir.x);
        animator.SetFloat("LastY", lastDir.y);
    }

    void OnEnable()
    {
        if (health != null)
            health.OnDeath += PlayDeath;
    }

    void OnDisable()
    {
        if (health != null)
            health.OnDeath -= PlayDeath;
    }

    private void PlayDeath()
    {
        animator.SetTrigger("Die");
    }
}
