using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class Error404ProcSystem : MonoBehaviour
{
    // Singleton for the auto-created runtime instance
    private static Error404ProcSystem _instance;
    private Coroutine _loop;

    [Header("Timing")]
    [SerializeField] private float minIntervalSeconds = 6f;
    [SerializeField] private float maxIntervalSeconds = 12f;
    [SerializeField] private bool waitBeforeFirstProc = true;

    [Header("Explosion")]
    [SerializeField] private int explosionDamage = 20;          // damage dealt to others in radius
    [SerializeField] private float explosionRadius = 1.5f;      // radius around the exploding enemy
    [SerializeField] private DamageElement element = DamageElement.Fire;

    [Header("Hitbox filtering")]
    [SerializeField] private LayerMask hitboxLayers;            // enemy hitbox layers. leave empty for all
    [SerializeField] private bool requireHitboxMarker = false;  // if true, only colliders with EnemyHitbox are valid

    [Header("VFX and audio")]
    [SerializeField] private GameObject explosionVfxPrefab;     // prefab with Animator (FB_Impact) + AudioSource
    [SerializeField] private AudioClip explosionSfxFallback;    // optional backup sound
    [SerializeField] private float explosionSfxVolume = 1f;

    [Header("Source filtering")]
    [SerializeField] private bool includeBossAsSource = false;  // usually false: only normal enemies are picked

    // --------------------------------------------------------------------------------
    // Lifecycle and public control
    // --------------------------------------------------------------------------------

    void Awake()
    {
        if (_instance == null) _instance = this;
        else if (_instance != this) { Destroy(gameObject); return; }
        DontDestroyOnLoad(gameObject);
    }

    // Ensure there is a running instance. Creates one if missing.
    private static Error404ProcSystem Ensure()
    {
        if (_instance != null) return _instance;

        var found = FindFirstObjectByType<Error404ProcSystem>(FindObjectsInactive.Exclude);
        if (found != null)
        {
            _instance = found;
            return _instance;
        }

        var go = new GameObject("Error404ProcSystem");
        _instance = go.AddComponent<Error404ProcSystem>();
        DontDestroyOnLoad(go);
        return _instance;
    }

    // Enable and inject the prefab and optional tunables from code
    public static void Enable(
        GameObject vfxPrefab,
        int? dmg = null,
        float? radius = null,
        LayerMask? layers = null,
        bool? requireMarker = null,
        float? minInt = null,
        float? maxInt = null,
        bool? waitFirst = null,
        bool? includeBoss = null)
    {
        var inst = Ensure();

        inst.explosionVfxPrefab = vfxPrefab;
        if (dmg.HasValue) inst.explosionDamage = dmg.Value;
        if (radius.HasValue) inst.explosionRadius = radius.Value;
        if (layers.HasValue) inst.hitboxLayers = layers.Value;
        if (requireMarker.HasValue) inst.requireHitboxMarker = requireMarker.Value;
        if (minInt.HasValue) inst.minIntervalSeconds = minInt.Value;
        if (maxInt.HasValue) inst.maxIntervalSeconds = maxInt.Value;
        if (waitFirst.HasValue) inst.waitBeforeFirstProc = waitFirst.Value;
        if (includeBoss.HasValue) inst.includeBossAsSource = includeBoss.Value;

        Debug.Log("[404] Enabled with VFX=" + (inst.explosionVfxPrefab ? inst.explosionVfxPrefab.name : "NULL"));

        if (inst._loop == null)
            inst._loop = inst.StartCoroutine(inst.ProcLoop());
    }

    public static void Disable()
    {
        if (_instance == null) return;
        if (_instance._loop != null)
        {
            _instance.StopCoroutine(_instance._loop);
            _instance._loop = null;
        }
    }

    // --------------------------------------------------------------------------------
    // Main loop
    // --------------------------------------------------------------------------------

    private IEnumerator ProcLoop()
    {
        if (waitBeforeFirstProc)
            yield return new WaitForSeconds(Random.Range(minIntervalSeconds, maxIntervalSeconds));

        while (true)
        {
            // If you also call Disable when the artifact is removed, no extra gating is needed here.
            ExplodeRandomEnemy();

            float delay = Random.Range(minIntervalSeconds, maxIntervalSeconds);
            yield return new WaitForSeconds(delay);
        }
    }

    // --------------------------------------------------------------------------------
    // Core explosion logic
    // --------------------------------------------------------------------------------

    private void ExplodeRandomEnemy()
    {
        var source = PickRandomSource();
        if (source == null) return;

        Vector2 pos = source.position;

        // Spawn impact VFX at z=0 to be sure it renders
        if (explosionVfxPrefab != null)
        {
            var vfx = Instantiate(explosionVfxPrefab, pos, Quaternion.identity);
            vfx.transform.position = new Vector3(pos.x, pos.y, 0f);
            Debug.Log("[404] Spawned explosion VFX at " + vfx.transform.position);
        }
        else
        {
            Debug.LogWarning("[404] explosionVfxPrefab is NULL when trying to spawn.");
        }

        // Optional backup sound if your VFX does not have an AudioSource or fails to play
        if (explosionSfxFallback != null)
            AudioSource.PlayClipAtPoint(explosionSfxFallback, new Vector3(pos.x, pos.y, 0f), explosionSfxVolume);

        // Damage others in radius
        int mask = (hitboxLayers.value == 0) ? Physics2D.AllLayers : hitboxLayers.value;
        var hits = Physics2D.OverlapCircleAll(pos, explosionRadius, mask);

        var processed = new HashSet<Transform>();
        var srcRoot = source.root;

        foreach (var h in hits)
        {
            if (requireHitboxMarker && h.GetComponent<EnemyHitbox>() == null)
                continue;

            var root = h.transform.root;
            if (!processed.Add(root)) continue;
            if (root == srcRoot) continue; // do not damage the source in the AoE

            var result = DamageCalculator.ComputeFinalDamage(explosionDamage, element);

            if (root.TryGetComponent(out EnemyHealth eh))
                eh.TakeDamage(result.amount, result.element);

            if (root.TryGetComponent(out BossHealth bh))
                bh.TakeDamage(result.amount, result.element);
        }

        // Kill the source enemy itself
        if (srcRoot.TryGetComponent(out EnemyHealth srcEh))
        {
            int hp = srcEh.GetHealth();
            var kill = DamageCalculator.ComputeFinalDamage(hp, element);
            srcEh.TakeDamage(kill.amount, kill.element);
        }
        else if (includeBossAsSource && srcRoot.TryGetComponent(out BossHealth srcBh))
        {
            var kill = DamageCalculator.ComputeFinalDamage(int.MaxValue, element);
            srcBh.TakeDamage(kill.amount, kill.element);
        }
    }

    private Transform PickRandomSource()
    {
        var enemies = FindObjectsByType<EnemyHealth>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        var bosses = includeBossAsSource
                    ? FindObjectsByType<BossHealth>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                    : System.Array.Empty<BossHealth>();

        var list = new List<Transform>();
        foreach (var e in enemies)
            if (e != null && e.GetHealth() > 0) list.Add(e.transform);

        if (includeBossAsSource)
            foreach (var b in bosses)
                list.Add(b.transform);

        if (list.Count == 0) return null;
        int i = Random.Range(0, list.Count);
        return list[i];
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, Mathf.Max(0.1f, explosionRadius));
    }
#endif
}
