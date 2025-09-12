using UnityEngine;
using System.Collections.Generic;

public class PlaceWall : MonoBehaviour, IAbilityBehavior
{
    [Header("Base Settings")]
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private Collider2D wallCollider;
    [SerializeField] private GameObject visual;

    [Header("Thorns (enabled from Rare+)")]
    [SerializeField] private bool thornsEnabled = false;
    [SerializeField] private int thornsDamage = 1;
    [SerializeField] private float thornsInterval = 0.5f;

    [Header("Lucky Shot (perpendicular duplicate)")]
    [Tooltip("Sideways distance (world units) to place the duplicate 90° to the aim.")]
    [SerializeField] private float duplicateSideOffset = 1.5f;
    [Tooltip("If true, spawn the duplicate to the RIGHT of aim; if false, to the LEFT.")]
    [SerializeField] private bool offsetToRight = true;

    private readonly Dictionary<Collider2D, float> nextTickTime = new Dictionary<Collider2D, float>();

    // Lucky Shot helpers
    private bool luckyWasDuplicated = false;   // prevents the duplicate from duplicating again
    private Vector2 castDir = Vector2.right;   // stored aim direction from Initialize

    public bool Initialize(Vector2 dir, Rarity rarity)
    {
        // keep aim so we know what "90 degrees" means
        castDir = (dir.sqrMagnitude > 0.0001f) ? dir.normalized : Vector2.right;

        // original rarity scaling
        switch (rarity)
        {
            case Rarity.Uncommon: lifetime *= 1.10f; break;
            case Rarity.Rare: lifetime *= 1.20f; thornsEnabled = true; thornsDamage = Mathf.Max(thornsDamage, 1); break;
            case Rarity.Epic: lifetime *= 1.30f; thornsEnabled = true; thornsDamage = Mathf.Max(thornsDamage, 2); thornsInterval *= 0.9f; break;
            case Rarity.Legendary: lifetime *= 1.40f; thornsEnabled = true; thornsDamage = Mathf.Max(thornsDamage, 3); thornsInterval *= 0.8f; break;
        }
        if (thornsInterval < 0.05f) thornsInterval = 0.05f;
        if (lifetime < 0.1f) lifetime = 0.1f;
        return true;
    }

    private void Start()
    {
        if (wallCollider != null) wallCollider.enabled = true;
        if (visual != null) visual.SetActive(true);
        Invoke(nameof(DestroySelf), lifetime);

        // Lucky Shot — spawn one more wall, 90° to aim, at inspector-set distance
        if (!luckyWasDuplicated)
        {
            LuckyShotSystem.OnSpellCast(this, () =>
            {
                Vector2 dir = (castDir.sqrMagnitude > 0.0001f) ? castDir : Vector2.right;

                // build perpendicular vectors: right/left of aim
                Vector2 rightOfAim = new Vector2(dir.y, -dir.x);
                Vector2 leftOfAim = new Vector2(-dir.y, dir.x);
                Vector2 side = offsetToRight ? rightOfAim : leftOfAim;

                Vector3 p2 = transform.position + (Vector3)(side.normalized * Mathf.Abs(duplicateSideOffset));

                var dup = Instantiate(gameObject, p2, transform.rotation);
                var comp = dup.GetComponent<PlaceWall>();
                if (comp != null)
                {
                    comp.luckyWasDuplicated = true; // don’t chain
                    comp.castDir = this.castDir;    // keep same aim for consistent side choice
                }
            });
        }
    }

    private void DestroySelf() => Destroy(gameObject);

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!thornsEnabled) return;
        TryTickThorns(collision.collider);
    }

    private void OnCollisionExit2D(Collision2D collision) => nextTickTime.Remove(collision.collider);
    private void OnTriggerStay2D(Collider2D other) { if (thornsEnabled) TryTickThorns(other); }
    private void OnTriggerExit2D(Collider2D other) => nextTickTime.Remove(other);

    private void TryTickThorns(Collider2D target)
    {
        bool isEnemy = target.TryGetComponent(out EnemyHealth enemy);
        bool isBoss = target.TryGetComponent(out BossHealth boss);
        if (!isEnemy && !isBoss) return;

        float now = Time.time;
        if (!nextTickTime.TryGetValue(target, out float next)) next = 0f;

        if (now >= next)
        {
            if (isEnemy) enemy.TakeDamage(thornsDamage, DamageElement.Physical);
            if (isBoss) boss.TakeDamage(thornsDamage, DamageElement.Physical);
            nextTickTime[target] = now + thornsInterval;
        }
    }
}
