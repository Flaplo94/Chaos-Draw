using UnityEngine;

public class PlayerCardInput : MonoBehaviour
{
    public GameObject aoePrefab;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            Instantiate(aoePrefab, transform.position, Quaternion.identity);
        }
    }
}
