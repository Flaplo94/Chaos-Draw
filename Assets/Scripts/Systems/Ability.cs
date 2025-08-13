using UnityEngine;

public enum Rarity { Common, Uncommon, Rare, Epic, Legendary }
public enum MagicType { Fire, Lightning, Other }

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

    [Header("Magic Type")]
    public MagicType magicType = MagicType.Other;

    public bool Activate()
    {
        // Compute spawn & direction (mouse or player)
        Vector3 spawnPos = Vector3.zero;
        Vector3 mouseWorld = Vector3.zero;
        Vector2 shootDir = Vector2.right;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) spawnPos = player.transform.position;

        if (UnityEngine.InputSystem.Mouse.current != null && Camera.main != null)
        {
            Vector2 mouseScreen = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
            mouseWorld = Camera.main.ScreenToWorldPoint(mouseScreen);
            mouseWorld.z = 0f;
            shootDir = (mouseWorld - spawnPos).normalized;
        }

        if (spawnAtMousePosition) spawnPos = mouseWorld;
        if (overrideDirection.HasValue) shootDir = overrideDirection.Value;

        // Must have a prefab with IAbilityBehavior
        if (effectPrefab == null) return false;

        GameObject obj = Instantiate(effectPrefab, spawnPos, Quaternion.identity);

        var behavior = obj.GetComponent<IAbilityBehavior>();
        if (behavior == null)
        {
            Debug.LogWarning($"Ability prefab '{effectPrefab.name}' is missing IAbilityBehavior.");
            Destroy(obj);
            return false; // don’t consume card
        }

        // Ask the ability behavior if it wants to activate (e.g., GodSpeed may veto if already active)
        bool ok = behavior.Initialize(shootDir, rarity);
        if (!ok)
        {
            // Veto: kill the instance and keep the card in hand, do not spend mana
            Destroy(obj);
            return false;
        }

        // Only now spend mana; if not enough, cancel and keep the card
        if (!PlayerMana.Instance.TrySpend(manaCost))
        {
            Destroy(obj);
            return false;
        }

        // Success caller (CardHandUI) will discard the card
        return true;
    }
}
