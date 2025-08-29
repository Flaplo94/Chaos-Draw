using System.Collections;
using UnityEngine;

public class CCOnTouch : MonoBehaviour
{
    [Header("Root effect")]
    [SerializeField] private float rootDuration = 1.2f;   // will be overridden by Setup if called
    [SerializeField] private LayerMask playerLayer;

    [Header("Hitbox lifetime")]
    [SerializeField] private float lifetime = 0.15f;      // short active window

    // Optional: controller calls this so we don't rely on inspector wiring
    public void Setup(LayerMask playerLayer, float rootDuration)
    {
        this.playerLayer = playerLayer;
        this.rootDuration = rootDuration;
    }

    private void OnEnable()
    {
        StartCoroutine(AutoDestroy());
    }

    private IEnumerator AutoDestroy()
    {
        yield return new WaitForSeconds(lifetime);
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // quick layer mask gate
        if (((1 << other.gameObject.layer) & playerLayer) == 0)
        {
            // if the collider is a child on a different layer, still try parent chain for the receiver
            var parentReceiver = other.GetComponentInParent<PlayerCrowdControlReceiver>();
            if (parentReceiver != null)
            {
                parentReceiver.LockMovement(rootDuration);
            }
            return;
        }

        // prefer hitting the receiver on parent (common when collider is a child)
        var receiver = other.GetComponentInParent<PlayerCrowdControlReceiver>();
        if (receiver != null)
        {
            receiver.LockMovement(rootDuration);
            return;
        }

        // fallback: any IMovementLock on the parent chain
        var mover = other.GetComponentInParent<IMovementLock>();
        if (mover != null)
        {
            mover.LockMovement(rootDuration);
        }
    }
}
