using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;

    private float baseSpeed;          // gemmer original v�rdi
    private Rigidbody2D rb;
    private Vector2 moveDir;

    // reference to CC lock state
    private PlayerCrowdControlReceiver crowdControlReceiver;

    [Header("Rooted Visual")]
    [SerializeField] private GameObject rootedIcon; // assign in inspector
    // Kun read?only adgang udefra (som du havde)
    public Vector2 MoveDir { get { return moveDir; } }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        baseSpeed = moveSpeed; // l�s �grundfart� til det, du har sat i Inspector
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.angularVelocity = 0f;
            rb.rotation = 0f;
        }

        crowdControlReceiver = GetComponent<PlayerCrowdControlReceiver>();
        if (crowdControlReceiver == null)
        {
            crowdControlReceiver = FindFirstObjectByType<PlayerCrowdControlReceiver>();
        }

        if (rootedIcon != null) rootedIcon.SetActive(false);
    }

    void Update()
    {
        bool locked = (crowdControlReceiver != null && crowdControlReceiver.IsMovementLocked());

        // toggle the visual
        if (rootedIcon != null)
            rootedIcon.SetActive(locked);

        if (locked)
        {
            moveDir = Vector2.zero;
            return;
        }

        HandleInput();
    }

    void FixedUpdate()
    {
        Move();
    }

    void LateUpdate()
    {
        if (!rb) return;

        rb.angularVelocity = 0f;
        rb.rotation = 0f;
        transform.rotation = Quaternion.identity;
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

        // 3) S�t velocity (og nul n�r ingen input, for at undg� drift)
        if (moveDir.sqrMagnitude > 0f)
        {
            rb.linearVelocity = moveDir * finalSpeed;   // brug rb.velocity hvis du bruger den klassiske 2D API
        }
        else
        {
            rb.linearVelocity = Vector2.zero;           // stop h�rdt n�r ingen input
        }
    }

    // (Valgfrit) hvis du senere vil �ndre grundfart fra andre systemer:
    public void SetBaseSpeed(float newBaseSpeed)
    {
        baseSpeed = Mathf.Max(0f, newBaseSpeed);
    }
}
