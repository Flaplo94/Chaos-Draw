using UnityEngine;

public enum Rarity { Common, Uncommon, Rare, Epic, Legendary }
public enum MagicType { Fire, Lightning, Utility }
public enum SmartcastMode { Auto, LineFromPlayer, CircleOnPlayer, CircleOnMouse, ConeFromPlayer }

[CreateAssetMenu(fileName = "NewAbility", menuName = "Abilities/Ability")]
public class Ability : ScriptableObject
{
    [Header("Identity")]
    public string abilityName;
    public Sprite icon;
    public GameObject effectPrefab;

    [Header("Spawn Settings")]
    public bool spawnAtMousePosition;
    [HideInInspector] public Vector2? overrideDirection;

    [Header("Ability Settings (UI + Balance)")]
    [Min(0)] public int damage = 0;               //  NYT felt til kort-UI
    [Min(0)] public int manaCost = 1;         // mana cost for at spille ability
    [TextArea] public string description;         // kortbeskrivelse til reward card

    [Header("Rarity")]
    public Rarity rarity = Rarity.Common;

    [Header("Magic Type (informativ – bruges i ability scripts)")]
    public MagicType magicType = MagicType.Utility;

    [Header("Smartcast Preview (optional)")]
    public SmartcastMode previewMode = SmartcastMode.Auto;
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

        // --- NEW: spend the SAME effective cost the UI shows (mult + flat, min 0) ---
        int effectiveCost = manaCost;
        var pbm = PlayerBuffManager.Instance;
        if (pbm != null)
        {
            float mult = 1f;
            float flat = 0f;

            // same mult used in CardHandUI.GetEffectiveManaCost()
            mult = pbm.GetManaCostReductionMult();

            // same flat used in CardHandUI (via reflection)
            try
            {
                var m = pbm.GetType().GetMethod("GetManaCostFlat");
                if (m != null && m.ReturnType == typeof(float))
                    flat = (float)m.Invoke(pbm, null);
            }
            catch { }

            float reduced = (manaCost / Mathf.Max(0.01f, mult)) - flat;
            effectiveCost = Mathf.Max(0, Mathf.CeilToInt(reduced));
        }

        // Brug mana efter vellykket init (med rabatten anvendt)
        if (!PlayerMana.Instance.TrySpend(effectiveCost))
        {
            Destroy(obj);
            return false;
        }

        return true;
    }

}
