using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(fileName = "NewAbility", menuName = "Abilities/Ability")]
public class Ability : ScriptableObject
{
    public string abilityName;
    public Sprite icon;
    public GameObject effectPrefab;
    public bool spawnAtMousePosition;
    [HideInInspector] public Vector2? overrideDirection;

    public void Activate()
    {
        Vector3 spawnPos = Vector3.zero;
        Vector3 mouseWorld = Vector3.zero;
        Vector2 shootDir = Vector2.right;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            spawnPos = player.transform.position;

        if (Mouse.current != null)
        {
            Vector2 mouseScreen = Mouse.current.position.ReadValue();
            mouseWorld = Camera.main.ScreenToWorldPoint(mouseScreen);
            mouseWorld.z = 0f;

            shootDir = (mouseWorld - spawnPos).normalized;
        }

        // Override spawn position if this ability is placed at the mouse
        if (spawnAtMousePosition)
        {
            spawnPos = mouseWorld;
        }

        if (effectPrefab != null)
        {
            GameObject obj = Instantiate(effectPrefab, spawnPos, Quaternion.identity);

            var fireball = obj.GetComponent<Fireball>();
            if (fireball != null)
            {
                fireball.SetDirection(shootDir);
                
            }
        }
    }

}
