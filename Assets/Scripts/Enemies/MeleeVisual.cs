using UnityEngine;

public class MeleeVisual : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private float visualScale = 1f; // make the animation bigger/smaller

    void Awake()
    {
        if (visualScale != 1f)
            transform.localScale *= visualScale;
    }

    // Called by BossAttackController right after spawn (optional helper)
    public void Setup(Vector2 worldDir)
    {
        if (worldDir.sqrMagnitude > 0.0001f)
        {
            // Face the player: align local +X with worldDir (works for top-down 2D sprites authored facing right)
            transform.right = new Vector3(-worldDir.x, -worldDir.y, 0f);
        }
    }

    // Animation Event on the FINAL frame of the melee animation
    public void EndFromAnimation()
    {
        Destroy(gameObject);
    }
}
