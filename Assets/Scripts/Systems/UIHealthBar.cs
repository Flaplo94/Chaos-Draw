using UnityEngine;
using UnityEngine.UI;

public class UIHealthBar : MonoBehaviour
{
    public static UIHealthBar Instance { get; private set; }

    private Slider slider;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        slider = GetComponent<Slider>();
    }

    public void SetValue(float normalizedValue)
    {
        slider.value = Mathf.Clamp01(normalizedValue);
    }
}