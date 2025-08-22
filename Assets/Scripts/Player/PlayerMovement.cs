using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Vector2 moveDir;
    public Vector2 MoveDir => moveDir; // kun denne public adgang

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;  // Unity 6 friendly
        rb.angularVelocity = 0f;  // clear any current spin
        rb.rotation = 0f;         // snap upright
    }

    void Update()
    {
        HandleInput();
    }

    void FixedUpdate()
    {
        Move();
    }
    void LateUpdate()
    {
        var rb = GetComponent<Rigidbody2D>();
        if (!rb) return;
        rb.angularVelocity = 0f;   // stop physics spin
        rb.rotation = 0f;          // keep facing upright
        transform.rotation = Quaternion.identity; // ensure no parent/child drift
    }

    void HandleInput()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");

        moveDir = new Vector2(moveX, moveY).normalized;
    }

    void Move()
    {
        rb.linearVelocity = moveDir * moveSpeed;
    }
}
