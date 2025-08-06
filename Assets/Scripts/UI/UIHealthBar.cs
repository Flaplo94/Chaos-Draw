using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UIHealthBar : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private Slider slider;

    [Header("Follow Settings")]
    [SerializeField] private Transform target;     // Player eller fjende
    [SerializeField] private Vector3 offset = new Vector3(0, 1f, 0);

    private Camera cam;

    private void Awake()
    {
        if (slider == null)
            slider = GetComponent<Slider>();

        cam = Camera.main;
    }

    private void OnEnable()
    {
        StartCoroutine(TrackPosition());
    }

    private IEnumerator TrackPosition()
    {
        while (true)
        {
            // Vent til slutningen af hvert frame  kameraet er færdig
            yield return new WaitForEndOfFrame();

            if (target != null && cam != null)
                transform.position = cam.WorldToScreenPoint(target.position + offset);
        }
    }

    public void SetValue(float normalizedValue)
    {
        slider.value = Mathf.Clamp01(normalizedValue);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}
