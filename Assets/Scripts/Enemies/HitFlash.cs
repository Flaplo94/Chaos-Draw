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

    private void Awake()
    {
        if (spriteRenderers == null || spriteRenderers.Length == 0)
            spriteRenderers = GetComponentsInChildren<SpriteRenderer>(includeInactive: true);

        _mpb = new MaterialPropertyBlock();
        SetFlash(0f);
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
        foreach (var sr in spriteRenderers)
        {
            if (!sr) continue;

            // IMPORTANT: fully define the block so we don't lose per-renderer sprite data
            _mpb.Clear();

            // Preserve the sprite texture and tint per renderer
            var sprite = sr.sprite;
            if (sprite != null)
                _mpb.SetTexture(MainTexID, sprite.texture);

            _mpb.SetColor(BaseColorID, sr.color);

            // Our flash value
            _mpb.SetFloat(FlashID, v);

            sr.SetPropertyBlock(_mpb);
        }
    }
}
