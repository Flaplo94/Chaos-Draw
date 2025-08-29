using UnityEngine;

/// Service der holder prefab til kyllinge-pet
public class PetSystemService : MonoBehaviour
{
    public static PetSystemService Instance;
    public GameObject chickenPetPrefab;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
}
