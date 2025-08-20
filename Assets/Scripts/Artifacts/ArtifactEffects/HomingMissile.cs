using UnityEngine;

/// Meget simpel homing adfaerd til banana-projektil
public class HomingMissile : MonoBehaviour
{
    public Transform target;
    public float speed = 8f;
    public float rotateSpeed = 360f;
    public float lifeTime = 8f;

    void Start() { Destroy(gameObject, lifeTime); }

    void Update()
    {
        if (target == null) { Destroy(gameObject); return; }

        Vector3 dir = (target.position - transform.position).normalized;
        float step = rotateSpeed * Mathf.Deg2Rad * Time.deltaTime;
        Vector3 newDir = Vector3.RotateTowards(transform.up, dir, step, 0f);
        transform.up = newDir;
        transform.position += transform.up * speed * Time.deltaTime;
    }
}
