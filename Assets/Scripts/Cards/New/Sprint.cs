using UnityEngine;

/// Attach this to a prefab and assign that prefab to Ability.effectPrefab
/// Base: +100% move speed (x2.0) for 3.0 seconds.
public class Sprint : MonoBehaviour, IAbilityBehavior
{
    public static Sprint Active; // prevent stacking; remove if you want multiple

    [Header("Core")]
    [SerializeField] private float duration = 3.0f;          // base duration (Common)
    [SerializeField] private float speedMultiplier = 2.0f;    // base = +100% (x2.0)

    [Header("Optional FX")]
    [SerializeField] private RuntimeAnimatorController auraController;
    [SerializeField] private string auraLoopState = "Loop";
    [SerializeField, Range(0f, 1f)] private float auraAlpha = 0.4f;

    [SerializeField] private AudioClip startSfx;
    [SerializeField] private AudioClip endSfx;
    [SerializeField] private AudioSource audioSource;

    private Transform player;
    private PlayerMovement playerMove;
    private float originalBaseSpeed;
    private GameObject auraHost;

    public bool Initialize(Vector2 _, Rarity rarity)
    {
        // prevent double-cast stacking; delete this block if you want stacking instead
        if (Active != null && Active != this)
        {
            var ui = FindFirstObjectByType<UIMessage>();
            if (ui) ui.ShowMessage("Sprint is already active");
            Destroy(gameObject);
            return false;
        }
        Active = this;

        // Rarity tweaks: same pattern as your other abilities (mild but noticeable bumps)
        switch (rarity)
        {
            case Rarity.Uncommon: duration += 0.5f; speedMultiplier *= 1.10f; break;
            case Rarity.Rare: duration += 1.0f; speedMultiplier *= 1.20f; break;
            case Rarity.Epic: duration += 1.5f; speedMultiplier *= 1.30f; break;
            case Rarity.Legendary: duration += 2.0f; speedMultiplier *= 1.40f; break;
        }
        duration = Mathf.Max(0.05f, duration);
        speedMultiplier = Mathf.Max(1f, speedMultiplier);
        return true;
    }

    private void Awake()
    {
        // Anchor to player like GodSpeed does
        var po = GameObject.FindGameObjectWithTag("Player");
        if (po != null)
        {
            transform.SetPositionAndRotation(po.transform.position, Quaternion.identity);
            transform.SetParent(po.transform, false);
        }
        // this object is just a host; hide any renderers/colliders on it
        foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
        foreach (var c in GetComponentsInChildren<Collider2D>()) c.enabled = false;

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (!playerObj) { DestroySelf(); return; }

        player = playerObj.transform;
        playerMove = playerObj.GetComponent<PlayerMovement>();
        if (!playerMove) { DestroySelf(); return; }

        // store + modify base speed (same API style as GodSpeed)
        originalBaseSpeed = playerMove.GetBaseSpeed();
        playerMove.SetBaseSpeed(originalBaseSpeed * speedMultiplier);

        PlayOneShot(startSfx);
        CreateOptionalAura();

        if (duration > 0f) Invoke(nameof(DestroySelf), duration);
    }

    private void CreateOptionalAura()
    {
        if (auraController == null) return;

        auraHost = new GameObject("Sprint_Aura");
        auraHost.transform.SetParent(player, false);

        var sr = auraHost.AddComponent<SpriteRenderer>();
        var c = sr.color; c.a = auraAlpha; sr.color = c;

        var anim = auraHost.AddComponent<Animator>();
        anim.runtimeAnimatorController = auraController;
        anim.updateMode = AnimatorUpdateMode.Normal;
        anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        anim.speed = 1f;
        anim.Rebind();
        anim.Update(0f);
        if (!string.IsNullOrEmpty(auraLoopState))
            anim.Play(auraLoopState, 0, 0f);
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (clip == null) return;
        if (audioSource == null)
        {
            // try to reuse player's AudioSource if present
            var pAS = player ? player.GetComponent<AudioSource>() : null;
            if (pAS != null) pAS.PlayOneShot(clip);
            return;
        }
        audioSource.PlayOneShot(clip);
    }

    private void OnDestroy()
    {
        // restore player speed
        if (playerMove != null)
            playerMove.SetBaseSpeed(originalBaseSpeed);

        PlayOneShot(endSfx);

        if (auraHost) Destroy(auraHost);
        if (Active == this) Active = null;
    }

    private void DestroySelf()
    {
        CancelInvoke();
        Destroy(gameObject);
    }
}
