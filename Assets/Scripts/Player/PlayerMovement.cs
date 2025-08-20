using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Vector2 moveDir;
    public Vector2 MoveDir { get { return moveDir; } } // kun denne public adgang

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        HandleInput();
    }

    void FixedUpdate()
    {
        Move();
    }

    void HandleInput()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");

        moveDir = new Vector2(moveX, moveY).normalized;
    }

    void Move()
    {
        float mult = 1f;
        if (PlayerBuffManager.Instance != null)
            mult = PlayerBuffManager.Instance.GetMoveSpeedMult(); // Swift Feet

        rb.linearVelocity = moveDir * moveSpeed * mult;
    }
}
