using UnityEngine;
using UnityEngine.UI;

public class SettingsSelectionSystemToggle : MonoBehaviour
{
    [SerializeField] private Toggle toggle;
    private const string Key = "SelectionSystemEnabled";

    void Start()
    {
        if (toggle == null) toggle = GetComponent<Toggle>();
        bool on = PlayerPrefs.GetInt(Key, 1) == 1; // default ON
        toggle.isOn = on;
        toggle.onValueChanged.AddListener(OnChanged);

        var ctrl = FindFirstObjectByType<CardSelectionController>();
        if (ctrl != null) ctrl.SetSelectionSystemEnabled(on);
    }

    private void OnChanged(bool on)
    {
        PlayerPrefs.SetInt(Key, on ? 1 : 0);
        PlayerPrefs.Save();

        var ctrl = FindFirstObjectByType<CardSelectionController>();
        if (ctrl != null) ctrl.SetSelectionSystemEnabled(on);
    }
}
