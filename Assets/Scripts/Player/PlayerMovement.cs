using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Base speed (uden buffs)")]
    [SerializeField] public float moveSpeed = 5f;

    private float baseSpeed;
    private Rigidbody2D rb;
    private Vector2 moveDir;

    public Vector2 MoveDir => moveDir;

    private PlayerCrowdControlReceiver crowdControlReceiver;

    [Header("Rooted Visual")]
    [SerializeField] private GameObject rootedIcon; // assign in inspector

    [Header("Input System")]
    [SerializeField] private InputActionAsset inputActions;

    private InputAction moveAction;

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
        baseSpeed = moveSpeed;
    }

    void OnEnable()
    {
        if (inputActions != null)
        {
            var playerMap = inputActions.FindActionMap("Player");
            moveAction = playerMap.FindAction("Move");

            if (moveAction != null)
                moveAction.Enable();
        }
    }

    void OnDisable()
    {
        if (moveAction != null)
            moveAction.Disable();
    }

    void Update()
    {
        bool locked = (crowdControlReceiver != null && crowdControlReceiver.IsMovementLocked());

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
        if (moveAction != null)
            moveDir = moveAction.ReadValue<Vector2>().normalized;
        else
            moveDir = Vector2.zero;
    }

    void Move()
    {
        float mult = 1f;
        if (PlayerBuffManager.Instance != null)
            mult = PlayerBuffManager.Instance.GetMoveSpeedMult();

        float finalSpeed = baseSpeed * mult;

        if (moveDir.sqrMagnitude > 0f)
        {
            rb.linearVelocity = moveDir * finalSpeed;
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    public void SetBaseSpeed(float newBaseSpeed)
    {
        baseSpeed = Mathf.Max(0f, newBaseSpeed);
    }

    public float GetBaseSpeed()
    {
        return baseSpeed;
    }
}
