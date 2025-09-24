using UnityEngine;
using UnityEngine.InputSystem;

public class InputInitializer : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;

    void Awake()
    {
        foreach (var map in inputActions.actionMaps)
        {
            string rebinds = PlayerPrefs.GetString(map.name + "_rebinds", "");
            if (!string.IsNullOrEmpty(rebinds))
                map.LoadBindingOverridesFromJson(rebinds);
        }
    }
}
