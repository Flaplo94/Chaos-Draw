using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ControlRebinder : MonoBehaviour
{
    [Header("Binding Target")]
    [SerializeField] private InputActionReference actionReference;
    [SerializeField] private int bindingIndex = 0;

    [Header("UI")]
    [SerializeField] private Button rebindButton;
    [SerializeField] private TMP_Text rebindLabel;

    // Maps to ignore when validating duplicates. Keep "UI" here.
    [Header("Duplicate Check")]
    [SerializeField] private string[] ignoreActionMaps = new string[] { "UI" };

    private InputAction action;
    private string lastOverrideBefore;
    private string lastEffectiveBefore;

    void Start()
    {
        action = actionReference != null ? actionReference.action : null;

        if (rebindButton != null)
            rebindButton.onClick.AddListener(StartRebind);

        if (action != null && bindingIndex < action.bindings.Count)
        {
            lastOverrideBefore = action.bindings[bindingIndex].overridePath;
            lastEffectiveBefore = action.bindings[bindingIndex].effectivePath;
        }

        UpdateLabel();
    }

    public void UpdateLabel()
    {
        if (rebindLabel == null || action == null) return;

        string s = action.GetBindingDisplayString(bindingIndex);

        // Only change Danish "Mellemrum" to English "Space"
        if (s.IndexOf("mellemrum", System.StringComparison.OrdinalIgnoreCase) >= 0)
            s = "Space";

        rebindLabel.text = s;
    }
    private void StartRebind()
    {
        if (action == null || bindingIndex >= action.bindings.Count) return;

        if (rebindLabel != null) rebindLabel.text = "Press a key...";

        lastOverrideBefore = action.bindings[bindingIndex].overridePath;
        lastEffectiveBefore = action.bindings[bindingIndex].effectivePath;

        action.Disable();

        action.PerformInteractiveRebinding(bindingIndex)
              .WithControlsExcluding("<Mouse>/position")
              .OnMatchWaitForAnother(0.1f)
              .OnComplete(op =>
              {
                  var picked = op.selectedControl;

                  // Nothing picked, restore and exit
                  if (picked == null)
                  {
                      op.Dispose();
                      action.Enable();
                      UpdateLabel();
                      return;
                  }

                  // Strict same-map duplicate check (ignores UI map)
                  if (ConflictsInSameMapIgnoreUI(picked, action, bindingIndex, ignoreActionMaps))
                  {
                      var message = $"{Humanize(picked.path)} is already in use in this action map!";
                      var uiMsg = Object.FindFirstObjectByType<UIMessage>();
                      if (uiMsg != null) uiMsg.ShowMessage(message);

                      // Restore previous binding (override or default)
                      if (!string.IsNullOrEmpty(lastOverrideBefore))
                          action.ApplyBindingOverride(bindingIndex, lastOverrideBefore);
                      else
                          action.RemoveBindingOverride(bindingIndex);

                      op.Dispose();
                      action.Enable();
                      UpdateLabel();
                      return;
                  }

                  // Accept
                  op.Dispose();
                  action.Enable();
                  UpdateLabel();

                  // Persist overrides for this map
                  var map = action.actionMap;
                  if (map != null)
                  {
                      string json = map.SaveBindingOverridesAsJson();
                      PlayerPrefs.SetString(map.name + "_rebinds", json);
                      PlayerPrefs.Save();
                  }
              })
              .Start();
    }

    private static bool ConflictsInSameMapIgnoreUI(
        InputControl pickedControl,
        InputAction selfAction,
        int selfBindingIndex,
        string[] ignoreMaps)
    {
        if (pickedControl == null || selfAction == null) return false;

        var selfMap = selfAction.actionMap;
        if (selfMap == null) return false;

        // If this map is in the ignore list, allow everything (edge case)
        if (IsIgnored(selfMap.name, ignoreMaps)) return false;

        // Only check inside the same map as the binding being edited.
        foreach (var act in selfMap.actions)
        {
            for (int i = 0; i < act.bindings.Count; i++)
            {
                // Skip the binding being edited
                if (act == selfAction && i == selfBindingIndex)
                    continue;

                var b = act.bindings[i];

                // Skip composite headers, but DO check composite parts
                if (b.isComposite) continue;

                var path = b.effectivePath;
                if (string.IsNullOrEmpty(path)) continue;

                // If this binding matches the picked control, it's a conflict
                if (InputControlPath.Matches(path, pickedControl))
                    return true;
            }
        }

        return false;
    }

    private static bool IsIgnored(string mapName, string[] ignoreMaps)
    {
        if (ignoreMaps == null) return false;
        for (int i = 0; i < ignoreMaps.Length; i++)
        {
            var s = ignoreMaps[i];
            if (!string.IsNullOrWhiteSpace(s) &&
                string.Equals(s.Trim(), mapName, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static string Humanize(string path)
    {
        return InputControlPath.ToHumanReadableString(
            path,
            InputControlPath.HumanReadableStringOptions.OmitDevice |
            InputControlPath.HumanReadableStringOptions.UseShortNames);
    }

    public static void ResetAll(InputActionAsset inputActions)
    {
        if (inputActions == null) return;

        foreach (var map in inputActions.actionMaps)
        {
            map.RemoveAllBindingOverrides();
            PlayerPrefs.DeleteKey(map.name + "_rebinds");
        }
        PlayerPrefs.Save();

        var rebinders = Object.FindObjectsByType<ControlRebinder>(FindObjectsSortMode.None);
        foreach (var r in rebinders) r.UpdateLabel();

        var uiMsg = Object.FindFirstObjectByType<UIMessage>();
        if (uiMsg != null) uiMsg.ShowMessage("Controls reset to defaults.");
    }
}
