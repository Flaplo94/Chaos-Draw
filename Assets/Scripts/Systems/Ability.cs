using UnityEngine;

public enum Rarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}

[CreateAssetMenu(fileName = "NewAbility", menuName = "Abilities/Ability")]
public class Ability : ScriptableObject
{
    public string abilityName;
    public Sprite icon;
    public GameObject effectPrefab;
    public bool spawnAtMousePosition;
    [HideInInspector] public Vector2? overrideDirection;

    [Header("Ability Settings")]
    public float manaCost = 10f;
    [TextArea] public string description;

    [Header("Rarity")]
    public Rarity rarity = Rarity.Common;

    public bool Activate()
    {
        if (!PlayerMana.Instance.TrySpend(manaCost)) return false;

        Vector3 spawnPos = Vector3.zero;
        Vector3 mouseWorld = Vector3.zero;
        Vector2 shootDir = Vector2.right;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) spawnPos = player.transform.position;

        if (UnityEngine.InputSystem.Mouse.current != null)
        {
            Vector2 mouseScreen = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
            mouseWorld = Camera.main.ScreenToWorldPoint(mouseScreen);
            mouseWorld.z = 0f;
            shootDir = (mouseWorld - spawnPos).normalized;
        }

        if (spawnAtMousePosition) spawnPos = mouseWorld;

        if (effectPrefab != null)
        {
            GameObject obj = Instantiate(effectPrefab, spawnPos, Quaternion.identity);

            // Use IAbilityBehavior interface to handle direction and rarity setup
            var behavior = obj.GetComponent<IAbilityBehavior>();
            if (behavior != null)
            {
                behavior.Initialize(shootDir, rarity);
            }
        }

        return true;
    }
}