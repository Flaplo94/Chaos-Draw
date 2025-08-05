using UnityEngine;

public class PlaceWall : MonoBehaviour
{
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private Collider2D wallCollider;
    [SerializeField] private GameObject visual;

    private void Start()
    {
        if (wallCollider != null)
            wallCollider.enabled = true;

        if (visual != null)
            visual.SetActive(true);

        Destroy(gameObject, lifetime);
    }
}