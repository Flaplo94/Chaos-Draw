using UnityEngine;

public class EnemyFollow : MonoBehaviour
{
    public float speed = 2f;
    private Transform player;

    void Start()
    {
        player = GameObject.FindWithTag("Player").transform;
    }

    void Update()
    {
        if (player == null) return;

        Vector2 direction = (player.position - transform.position).normalized;
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
    }

    private void OnDestroy()
    {
        // Tjek at PlayerXP er tilg�ngelig, f�r vi kalder det
        if (PlayerXP.Instance != null)
        {
            PlayerXP.Instance.GainXP(1);
        }
    }
}
