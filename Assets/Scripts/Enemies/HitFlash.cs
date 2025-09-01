using UnityEngine;
using System.Collections;

[DisallowMultipleComponent]
public class HitFlash : MonoBehaviour
{
    [SerializeField] private SpriteRenderer[] spriteRenderers;
    [SerializeField] private float flashUpTime = 0.03f;
    [SerializeField] private float flashHoldTime = 0.04f;
    [SerializeField] private float flashDownTime = 0.10f;

    private static readonly int FlashID = Shader.PropertyToID("_FlashAmount");
    private static readonly int MainTexID = Shader.PropertyToID("_MainTex");
    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");

    private MaterialPropertyBlock _mpb;
    private float currentFlash = 0f;
    private Sprite[] lastSprites; // track sprite swaps

    private void Awake()
    {
        if (spriteRenderers == null || spriteRenderers.Length == 0)
            spriteRenderers = GetComponentsInChildren<SpriteRenderer>(includeInactive: true);

        _mpb = new MaterialPropertyBlock();
        lastSprites = new Sprite[spriteRenderers.Length];
        SetFlash(0f);
    }

    private void LateUpdate()
    {
        // Only refresh when the sprite changes
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            var sr = spriteRenderers[i];
            if (!sr) continue;

            if (sr.sprite != lastSprites[i])
            {
                lastSprites[i] = sr.sprite;
                ApplyFlash(sr, currentFlash);
            }
        }
    }

    public void PlayFlash()
    {
        StopAllCoroutines();
        StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        yield return LerpFlash(0f, 1f, flashUpTime);
        if (flashHoldTime > 0f) yield return new WaitForSeconds(flashHoldTime);
        yield return LerpFlash(1f, 0f, flashDownTime);
        SetFlash(0f);
    }

    private IEnumerator LerpFlash(float from, float to, float time)
    {
        if (time <= 0f) { SetFlash(to); yield break; }

        float t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            SetFlash(Mathf.Lerp(from, to, t / time));
            yield return null;
        }
        SetFlash(to);
    }

    private void SetFlash(float v)
    {
        currentFlash = v;

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            ApplyFlash(spriteRenderers[i], v);
            lastSprites[i] = spriteRenderers[i]?.sprite;
        }
    }

    private void ApplyFlash(SpriteRenderer sr, float v)
    {
        if (!sr) return;

        _mpb.Clear();

        var sprite = sr.sprite;
        if (sprite != null)
            _mpb.SetTexture(MainTexID, sprite.texture);

        _mpb.SetColor(BaseColorID, sr.color);
        _mpb.SetFloat(FlashID, v);

        sr.SetPropertyBlock(_mpb);
    }
}
