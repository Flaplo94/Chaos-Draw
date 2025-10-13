using UnityEngine;
using System.Reflection;

/// Attach this to a prefab and assign that prefab to Ability.effectPrefab.
/// Base: -50% DAMAGE TAKEN for 3.0s, and -20% movespeed while active.
public class Stoneform : MonoBehaviour, IAbilityBehavior
{
    public static Stoneform Active; // prevent stacking; delete this if you want overlapping Stoneforms

    [Header("Core")]
    [SerializeField] private float duration = 3.0f;     // base duration (Common)
    [SerializeField, Range(0.1f, 1f)]
    private float damageTakenMultiplier = 0.5f;         // 0.5 = take half damage
    [SerializeField, Range(0.1f, 1f)]
    private float moveSpeedMultiplier = 0.8f;           // 0.8 = -20% move

    [Header("Optional FX")]
    [SerializeField] private RuntimeAnimatorController auraController;
    [SerializeField] private string auraLoopState = "Loop";
    [SerializeField, Range(0f, 1f)] private float auraAlpha = 0.45f;
    [SerializeField] private AudioClip startSfx;
    [SerializeField] private AudioClip endSfx;
    [SerializeField] private AudioSource audioSource;

    private Transform player;
    private PlayerMovement playerMove;

    // movement restore
    private float originalBaseSpeed;

    // damage-taken hook bookkeeping
    private PlayerHealth playerHealth;

    public bool Initialize(Vector2 _, Rarity rarity)
    {
        // single-instance guard; remove if you want stacking
        if (Active != null && Active != this)
        {
            var ui = FindFirstObjectByType<UIMessage>();
            if (ui) ui.ShowMessage("Stoneform is already active");
            Destroy(gameObject);
            return false;
        }
        Active = this;

        // Rarity scaling: keep mitigation constant; extend duration slightly with rarity
        switch (rarity)
        {
            case Rarity.Uncommon: duration += 0.5f; break;
            case Rarity.Rare: duration += 1.0f; break;
            case Rarity.Epic: duration += 1.5f; break;
            case Rarity.Legendary: duration += 2.0f; break;
        }
        duration = Mathf.Max(0.05f, duration);
        return true;
    }

    private void Awake()
    {
        var po = GameObject.FindGameObjectWithTag("Player");
        if (po != null)
        {
            transform.SetPositionAndRotation(po.transform.position, Quaternion.identity);
            transform.SetParent(po.transform, false);
        }
        foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
        foreach (var c in GetComponentsInChildren<Collider2D>()) c.enabled = false;

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (!playerObj) { DestroySelf(); return; }

        playerHealth = playerObj.GetComponent<PlayerHealth>();
        var move = playerObj.GetComponent<PlayerMovement>();
        if (!playerHealth || !move) { DestroySelf(); return; }

        // movement penalty
        originalBaseSpeed = move.GetBaseSpeed();
        move.SetBaseSpeed(originalBaseSpeed * 0.8f); // -20%

        // damage reduction: take 50% damage => multiplier = 0.5
        playerHealth.AddIncomingDamageMultiplier(this, 0.5f);

        // sfx/vfx + timer as you already had
        if (duration > 0f) Invoke(nameof(DestroySelf), duration);
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (clip == null) return;
        if (audioSource == null)
        {
            var pAS = player ? player.GetComponent<AudioSource>() : null;
            if (pAS != null) pAS.PlayOneShot(clip);
            return;
        }
        audioSource.PlayOneShot(clip);
    }

    private void OnDestroy()
    {
        // restore movement
        if (player != null)
        {
            var move = player.GetComponent<PlayerMovement>();
            if (move) move.SetBaseSpeed(originalBaseSpeed);
        }
        // restore damage-taken
        if (playerHealth) playerHealth.RemoveIncomingDamageMultiplier(this);

        PlayOneShot(endSfx);
        if (Active == this) Active = null;
    }

    private void DestroySelf()
    {
        CancelInvoke();
        Destroy(gameObject);
    }
}
