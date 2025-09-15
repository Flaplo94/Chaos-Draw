using UnityEngine;

[DisallowMultipleComponent]
public class ExplosionVFXController : MonoBehaviour
{
    [Header("Animator")]
    [SerializeField] private Animator animator;                 // auto-found
    [SerializeField] private string stateName = "FB_Impact";    // your clip/state
    [SerializeField] private bool useUnscaledTime = true;
    [SerializeField] private AnimatorCullingMode cullingMode = AnimatorCullingMode.AlwaysAnimate;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;           // auto-found
    [SerializeField] private AudioClip sfxOverride;             // leave null to use AudioSource.clip
    [SerializeField] private bool force2D = true;               // spatialBlend = 0
    

    [Header("Render (optional helpers)")]
    [SerializeField] private int sortingOrderBoost = 100;       // pushes above enemies

    [Header("Lifetime")]
    [SerializeField] private float extraLifetime = 0.05f;

    void Awake()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr) sr.sortingOrder += sortingOrderBoost;

        if (!animator) animator = GetComponent<Animator>();
        if (animator)
        {
            animator.updateMode = useUnscaledTime ? AnimatorUpdateMode.UnscaledTime : AnimatorUpdateMode.Normal;
            animator.cullingMode = cullingMode;
            if (!string.IsNullOrEmpty(stateName))
            {
                animator.Play(stateName, 0, 0f);
                Debug.Log($"[404 VFX] Played state '{stateName}'.");
            }
        }
        else
        {
            Debug.LogWarning("[404 VFX] No Animator found.");
        }

        if (!audioSource) audioSource = GetComponent<AudioSource>();
        if (audioSource)
        {
            if (force2D) audioSource.spatialBlend = 0f;
            var clip = sfxOverride ? sfxOverride : audioSource.clip;
            if (clip) { audioSource.PlayOneShot(clip); Debug.Log($"[404 VFX] Played SFX '{clip.name}'."); }
            else Debug.LogWarning("[404 VFX] No audio clip to play.");
        }
        else
        {
            Debug.LogWarning("[404 VFX] No AudioSource found.");
        }

        // Destroy after exact clip length if available
        float len = 0.3f;
        if (animator && animator.runtimeAnimatorController)
        {
            foreach (var c in animator.runtimeAnimatorController.animationClips)
                if (c && c.name == stateName) { len = c.length; break; }
        }
        Destroy(gameObject, len + extraLifetime);
    }

    void OnImpactFinished()
    {
        Destroy(gameObject);
    }
    void PlayImpactSound()
    {
        
    }
}
