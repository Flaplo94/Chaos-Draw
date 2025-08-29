using UnityEngine;

/// Simpel orbiter der kredser om en ejer
public class SimpleOrbiter : MonoBehaviour
{
    public Transform owner;
    public float radius = 1.8f;
    public float angularSpeed = 120f; // grader pr. sek
    public float angleDeg;

    void Update()
    {
        if (owner == null) { Destroy(gameObject); return; }
        angleDeg += angularSpeed * Time.deltaTime;
        float rad = angleDeg * Mathf.Deg2Rad;
        Vector3 center = owner.position;
        Vector3 pos = center + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * radius;
        transform.position = pos;
    }
}
