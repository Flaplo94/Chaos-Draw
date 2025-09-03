using UnityEngine;
using UnityEngine.UI;

public class AttackCooldownIndicator : MonoBehaviour
{
    [SerializeField] private Image cooldownImage;
    [SerializeField] private Transform target;  // Player
    [SerializeField] private Vector3 worldOffset = new Vector3(1f, 1f, 0);

    private float cooldownDuration;
    private float timer;
    private Camera mainCam;

    private void Awake()
    {
        mainCam = Camera.main;
    }

    public void StartCooldown(float duration, Transform followTarget)
    {
        target = followTarget;
        cooldownDuration = duration;
        timer = duration;
        cooldownImage.fillAmount = 1f;
        gameObject.SetActive(true);
    }

    private void Update()
    {
        if (timer > 0f)
        {
            timer -= Time.deltaTime;
            float t = Mathf.Clamp01(timer / cooldownDuration);
            cooldownImage.fillAmount = t;
        }
        else if (gameObject.activeSelf)
        {
            gameObject.SetActive(false);
        }

        // follow player in screen space
        if (target != null && mainCam != null)
        {
            Vector3 screenPos = mainCam.WorldToScreenPoint(target.position + worldOffset);
            transform.position = screenPos;
        }
    }
}
