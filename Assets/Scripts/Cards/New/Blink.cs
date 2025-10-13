using UnityEngine;

// Put this on a small empty prefab (no collider required).
// Your Ability asset should reference that prefab as effectPrefab.
public class Blink : MonoBehaviour, IAbilityBehavior
{
    [Header("Blink Settings")]
    [SerializeField] private float range = 5f;         // base blink distance
    [SerializeField] private float skin = 0.08f;             // keep a tiny gap from walls
    [SerializeField] private float safetyRadius = 0.25f;     // clearance circle for the player capsule
    [SerializeField] private LayerMask collisionMask;         // e.g. GroundObstacles, Walls
    [SerializeField] private float selfDestructAfter = 0.02f; // prefab cleans up instantly

    [Header("Optional")]
    [SerializeField] private bool zeroVelocityOnArrival = true; // stop any RB drift

    private Vector2 cachedDir = Vector2.right;

    // Initialize is called by Ability.Activate() after spawning this prefab.
    public bool Initialize(Vector2 dir, Rarity rarity)
    {
        // Find player by tag (matches your Ability.cs pattern)
        GameObject playerGo = GameObject.FindGameObjectWithTag("Player");
        if (playerGo == null)
        {
            Debug.LogWarning("[Blink] No Player found (tag=Player).");
            return false;
        }

        Transform player = playerGo.transform;

        // Direction: use provided aim dir; if zero, default to facing/right
        cachedDir = (dir.sqrMagnitude > 0.0001f) ? dir.normalized : Vector2.right;

        // Rarity scaling (same style as your AlmightyPush)
        float distance = range;
        switch (rarity)
        {
            case Rarity.Uncommon: distance *= 1.10f; break;
            case Rarity.Rare: distance *= 1.25f; break;
            case Rarity.Epic: distance *= 1.40f; break;
            case Rarity.Legendary: distance *= 1.60f; break;
        }

        // Compute target using a circle cast so we don't blink inside walls.
        Vector2 start = player.position;
        Vector2 dirNorm = cachedDir;

        float allowed = distance;

        if (collisionMask.value != 0)
        {
            // CircleCast from current position toward aim direction
            RaycastHit2D hit = Physics2D.CircleCast(start, safetyRadius, dirNorm, distance, collisionMask);
            if (hit.collider != null)
            {
                // stop just before the obstacle
                allowed = Mathf.Max(0f, hit.distance - skin);
            }
        }

        Vector2 end = start + dirNorm * allowed;

        // Teleport the player
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.position = end;
            if (zeroVelocityOnArrival)
                rb.linearVelocity = Vector2.zero; // Unity 6 API
        }
        else
        {
            player.position = end;
        }

        // Optional: spawn a tiny flash/VFX here if you want (particle, sound, etc.)

        if (selfDestructAfter > 0f) Destroy(gameObject, selfDestructAfter);
        else Destroy(gameObject);

        return true;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Visualize the blink arc from this prefab's position (editor aid)
        Gizmos.color = new Color(0.2f, 0.7f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, safetyRadius);
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)(Vector2.right * range));
    }
#endif
}
