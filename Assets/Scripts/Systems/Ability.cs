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

    [Header("Magic Type (informativ – bruges i selve ability scripts)")]
    public MagicType magicType = MagicType.Other;

    public bool Activate()
    {
        // Find player + udgangspunkt
        Vector3 spawnPos = Vector3.zero;
        Vector3 mouseWorld = Vector3.zero;
        Vector2 shootDir = Vector2.right;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) spawnPos = player.transform.position;

        // Museretning, hvis tilgængelig
        if (UnityEngine.InputSystem.Mouse.current != null && Camera.main != null)
        {
            Vector2 mouseScreen = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
            mouseWorld = Camera.main.ScreenToWorldPoint(mouseScreen);
            mouseWorld.z = 0f;
            shootDir = (mouseWorld - spawnPos).normalized;
        }

        if (spawnAtMousePosition) spawnPos = mouseWorld;
        if (overrideDirection.HasValue) shootDir = overrideDirection.Value;

        if (effectPrefab == null)
        {
            Debug.LogWarning($"Ability '{name}' has no effectPrefab assigned.");
            return false;
        }

        // Spawn og init
        GameObject obj = Instantiate(effectPrefab, spawnPos, Quaternion.identity);

        var behavior = obj.GetComponent<IAbilityBehavior>();
        if (behavior == null)
        {
            Debug.LogWarning($"Ability prefab '{effectPrefab.name}' is missing IAbilityBehavior.");
            Destroy(obj);
            return false; // forbrug ikke mana
        }

        // Lad ability’en selv håndtere element (f.eks. Fireball = DamageElement.Fire)
        bool ok = behavior.Initialize(shootDir, rarity);
        if (!ok)
        {
            Destroy(obj);
            return false;
        }

        // Brug mana efter vellykket init (samme adfærd som før)
        if (!PlayerMana.Instance.TrySpend(manaCost))
        {
            Destroy(obj);
            return false;
        }

        return true;
    }
}
