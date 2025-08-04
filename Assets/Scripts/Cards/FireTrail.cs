using UnityEngine;
using System.Collections;

public class FireTrail : MonoBehaviour
{
    [SerializeField] private GameObject firePrefab; // FireOnGround prefab
    [SerializeField] private float duration = 3f;
    [SerializeField] private float dropInterval = 0.3f;

    private Transform player;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        StartCoroutine(LeaveTrail());
    }

    private IEnumerator LeaveTrail()
    {
        float timer = 0f;

        while (timer < duration)
        {
            Instantiate(firePrefab, player.position, Quaternion.identity);
            yield return new WaitForSeconds(dropInterval);
            timer += dropInterval;
        }

        Destroy(gameObject); // remove the trail controller
    }
}
