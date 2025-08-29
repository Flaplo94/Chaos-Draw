using UnityEngine;

public class GlowPulse : MonoBehaviour
{
    public float speed = 2f;       // hastighed på pulsen
    public float minScale = 0.95f; // hvor lille den bliver
    public float maxScale = 1.05f; // hvor stor den bliver

    private Vector3 baseScale;

    void Awake()
    {
        baseScale = transform.localScale;
    }

    void Update()
    {
        float t = (Mathf.Sin(Time.time * speed) + 1f) / 2f; // 0–1 pingpong
        float scale = Mathf.Lerp(minScale, maxScale, t);
        transform.localScale = baseScale * scale;
    }
}
