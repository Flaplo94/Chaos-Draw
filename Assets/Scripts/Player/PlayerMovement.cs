using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Base speed (uden buffs)")]
    [SerializeField] private float moveSpeed = 5f;

    private float baseSpeed;          // gemmer original værdi
    private Rigidbody2D rb;
    private Vector2 moveDir;

    // Kun read?only adgang udefra (som du havde)
    public Vector2 MoveDir { get { return moveDir; } }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        baseSpeed = moveSpeed; // lås “grundfart” til det, du har sat i Inspector
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
        // 1) Hent multiplier (default = 1 hvis manager mangler)
        float mult = 1f;
        if (PlayerBuffManager.Instance != null)
            mult = PlayerBuffManager.Instance.GetMoveSpeedMult();

        // 2) Regn den endelige fart
        float finalSpeed = baseSpeed * mult;

        // 3) Sæt velocity (og nul når ingen input, for at undgå drift)
        if (moveDir.sqrMagnitude > 0f)
        {
            rb.linearVelocity = moveDir * finalSpeed;   // brug rb.velocity hvis du bruger den klassiske 2D API
        }
        else
        {
            rb.linearVelocity = Vector2.zero;           // stop hårdt når ingen input
        }
    }

    // (Valgfrit) hvis du senere vil ændre grundfart fra andre systemer:
    public void SetBaseSpeed(float newBaseSpeed)
    {
        baseSpeed = Mathf.Max(0f, newBaseSpeed);
    }
}
