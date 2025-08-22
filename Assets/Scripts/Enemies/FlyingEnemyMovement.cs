using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class FlyingEnemy : MonoBehaviour
{
    [SerializeField] private float speed = 3f;

    private Transform player;
    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0; // Just to be safe
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;  // Unity 6 friendly
        rb.angularVelocity = 0f;  // clear any current spin
        rb.rotation = 0f;         // snap upright
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
    }

    void Update()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
            else
                return;
        }

        Vector2 direction = (player.position - transform.position).normalized;
        rb.linearVelocity = direction * speed;
    }
    void LateUpdate()
    {
        var rb = GetComponent<Rigidbody2D>();
        if (!rb) return;
        rb.angularVelocity = 0f;   // stop physics spin
        rb.rotation = 0f;          // keep facing upright
        transform.rotation = Quaternion.identity; // ensure no parent/child drift
    }
} 