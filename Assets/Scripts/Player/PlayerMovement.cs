using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Vector2 moveDir;
    public Vector2 MoveDir => moveDir; // kun denne public adgang

    // reference to CC lock state
    private PlayerCrowdControlReceiver crowdControlReceiver;

    [Header("Rooted Visual")]
    [SerializeField] private GameObject rootedIcon; // assign in inspector

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
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
        if (rb != null)
            rb.linearVelocity = moveDir * moveSpeed;
    }
}
