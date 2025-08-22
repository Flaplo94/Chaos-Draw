using UnityEngine;
using UnityEngine.Rendering; // SortingGroup

public class GodSpeed : MonoBehaviour, IAbilityBehavior
{
    public static GodSpeed Active;

    [Header("Base")]
    [SerializeField] private float duration = 5f;
    [SerializeField] private float speedMultiplier = 1.5f;

    [Header("Zap")]
    [SerializeField] private float zapRadius = 1.0f;
    [SerializeField] private int zapDamagePerTick = 1;
    [SerializeField] private float zapTickInterval = 0.12f;
    [SerializeField] private LayerMask enemyLayer; // 0 = no filter

    [Header("LightningBall ANIMATOR (no prefab needed)")]
    [Tooltip("Assign your LightningBall Animator Controller here.")]
    [SerializeField] private RuntimeAnimatorController auraController;
    [Tooltip("Animator trigger to start (optional). Leave empty to play a state by name.")]
    [SerializeField] private string auraTrigger = "";
    [Tooltip("Looping state name inside the LightningBall controller, e.g. 'Ball_Loop'.")]
    [SerializeField] private string auraLoopState = "Ball_Loop";
    [Tooltip("Animator.speed for the aura animation.")]
    [SerializeField] private float auraAnimSpeed = 1f;


    [Header("Bolt Visual (Animator + SpriteRenderer prefab)")]
    [SerializeField] private GameObject lightningVisual;
    [SerializeField] private string boltTrigger = "Play";
    [SerializeField] private string boltStateName = "Zap";
    [SerializeField] private float boltWidthScale = 1f;
    [SerializeField] private float boltAnimSpeed = 1.0f;

    [Header("Aura Visual")]
    [SerializeField] private float auraScale = 1.5f;     // make the aura bigger
    [SerializeField, Range(0f, 1f)] private float auraAlpha = 0.7f; // control transparency


    private Transform player;
    private PlayerMovement playerMove;
    private float originalMoveSpeed;
    private float zapTimer;

    // Runtime-created visual host for the LightningBall controller
    private GameObject auraHost;

    public bool Initialize(Vector2 _, Rarity rarity)
    {
        // Prevent double-activation
        if (Active != null && Active != this)
        {
            var ui = FindFirstObjectByType<UIMessage>();
            if (ui) ui.ShowMessage("God Speed is already active");
            Destroy(gameObject);
            return false;
        }
        Active = this;

        // Rarity scaling
        switch (rarity)
        {
            case Rarity.Uncommon: duration += 1f; speedMultiplier *= 1.10f; zapRadius *= 1.05f; break;
            case Rarity.Rare: duration += 2f; speedMultiplier *= 1.20f; zapRadius *= 1.10f; zapDamagePerTick += 1; break;
            case Rarity.Epic: duration += 3f; speedMultiplier *= 1.30f; zapRadius *= 1.15f; zapDamagePerTick += 2; break;
            case Rarity.Legendary: duration += 4f; speedMultiplier *= 1.40f; zapRadius *= 1.20f; zapDamagePerTick += 3; break;
        }
        if (duration < 0f) duration = 0f;
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

        // Holder is invisible
        foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
        foreach (var c in GetComponentsInChildren<Collider2D>()) c.enabled = false;
    }

    private void Start()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (!playerObj) { DestroySelf(); return; }

        player = playerObj.transform;
        playerMove = playerObj.GetComponent<PlayerMovement>();
        if (!playerMove) { DestroySelf(); return; }

        // Speed buff
        originalMoveSpeed = playerMove.moveSpeed;
        playerMove.moveSpeed = originalMoveSpeed * Mathf.Max(1f, speedMultiplier);

        // Create the visual host at runtime that uses the LightningBall Animator Controller
        CreateAuraHost();

        if (duration > 0f) Invoke(nameof(DestroySelf), duration);
    }

    private void Update()
    {
        if (!player) { DestroySelf(); return; }

        zapTimer += Time.deltaTime;
        if (zapTimer >= zapTickInterval)
        {
            zapTimer = 0f;
            ZapNearby();
        }
    }

    private void CreateAuraHost()
    {
        auraHost = new GameObject("GodSpeed_AuraHost");
        auraHost.transform.SetParent(player, false);
        auraHost.transform.localPosition = Vector3.zero;
        auraHost.transform.localRotation = Quaternion.identity;
        auraHost.transform.localScale = Vector3.one * auraScale;

        var sr = auraHost.AddComponent<SpriteRenderer>();
        sr.enabled = true;
        sr.color = new Color(1f, 1f, 1f, auraAlpha); // set transparency

        var anim = auraHost.AddComponent<Animator>();
        anim.runtimeAnimatorController = auraController;
        anim.speed = Mathf.Max(0.01f, auraAnimSpeed);
        anim.Rebind();
        anim.Update(0f);

        if (!string.IsNullOrEmpty(auraTrigger))
            anim.SetTrigger(auraTrigger);
        else if (!string.IsNullOrEmpty(auraLoopState))
            anim.Play(auraLoopState, 0, 0f);
    }

    private void ZapNearby()
    {
        var hits = (enemyLayer.value == 0)
            ? Physics2D.OverlapCircleAll(player.position, zapRadius)
            : Physics2D.OverlapCircleAll(player.position, zapRadius, enemyLayer);

        if (hits == null || hits.Length == 0) return;

        foreach (var h in hits)
        {
            if (!h) continue;

            Transform root = h.attachedRigidbody ? h.attachedRigidbody.transform : h.transform;
            if (root == player) continue;
            if (root == transform || root.IsChildOf(transform)) continue;

            bool didDamage = false;
            if (h.TryGetComponent(out EnemyHealth eh)) { eh.TakeDamage(zapDamagePerTick); didDamage = true; }
            if (h.TryGetComponent(out BossHealth bh)) { bh.TakeDamage(zapDamagePerTick); didDamage = true; }
            if (!didDamage) continue;

            if (lightningVisual) SpawnBolt(player.position, root.position);
        }
    }

    private void SpawnBolt(Vector3 from, Vector3 to)
    {
        var go = Instantiate(lightningVisual);
        go.name = "GodSpeed_Bolt";

        // midpoint & rotation
        Vector3 mid = (from + to) * 0.5f;
        go.transform.position = mid;

        Vector2 dir = (to - from);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        go.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        // stretch along X to length
        float length = dir.magnitude;
        var sr = go.GetComponentInChildren<SpriteRenderer>();
        if (sr && sr.sprite)
        {
            float currentWidth = sr.bounds.size.x;
            if (currentWidth <= 0f)
                currentWidth = sr.sprite.rect.width / sr.sprite.pixelsPerUnit * go.transform.lossyScale.x;

            if (currentWidth > 0f)
            {
                float mul = length / currentWidth;
                go.transform.localScale = new Vector3(go.transform.localScale.x * mul,
                                                       go.transform.localScale.y * boltWidthScale,
                                                       go.transform.localScale.z);
            }
            else
            {
                go.transform.localScale = new Vector3(length, boltWidthScale, 1f);
            }
        }
        else
        {
            go.transform.localScale = new Vector3(length, boltWidthScale, 1f);
        }

        // animate + cleanup
        float ttl = 0.25f;
        var anim = go.GetComponentInChildren<Animator>();
        if (anim)
        {
            anim.speed = Mathf.Max(0.01f, boltAnimSpeed);
            anim.Rebind();
            anim.Update(0f);

            if (!string.IsNullOrEmpty(boltTrigger))
            {
                anim.ResetTrigger(boltTrigger);
                anim.SetTrigger(boltTrigger);
            }
            else if (!string.IsNullOrEmpty(boltStateName))
            {
                anim.Play(boltStateName, 0, 0f);
            }

            var st = anim.GetCurrentAnimatorStateInfo(0);
            ttl = st.length > 0f ? st.length / anim.speed : ttl;
        }

        Destroy(go, ttl);
    }

    private void OnDestroy()
    {
        if (playerMove != null) playerMove.moveSpeed = originalMoveSpeed;
        if (auraHost) Destroy(auraHost);
        if (Active == this) Active = null;
    }

    private void DestroySelf()
    {
        CancelInvoke();
        Destroy(gameObject);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (player != null)
        {
            Gizmos.color = new Color(0.6f, 0.9f, 1f, 0.35f);
            Gizmos.DrawWireSphere(player.position, zapRadius);
        }
    }
#endif
}
