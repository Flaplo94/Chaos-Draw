using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-32000)] // endnu tidligere end før
public class DuplicateComponentKiller : MonoBehaviour
{
    public enum KeepStrategy { Oldest, Newest, InActiveScene }

    [Header("Select the component type to guard")]
    [Tooltip("Drag the component you want to enforce as single-instance (e.g., PlayerBuffManager) onto this field.")]
    [SerializeField] private Component targetComponent;

    [Header("Which instance should survive if duplicates exist?")]
    public KeepStrategy keep = KeepStrategy.InActiveScene;

    private void Awake()
    {
        if (targetComponent == null)
        {
            // Fail-safe: vælg første “rigtige” komponent på GO’et
            var first = GetComponents<Component>()
                .FirstOrDefault(c => !(c is Transform) && !(c is DuplicateComponentKiller));
            if (first == null)
            {
                Debug.LogWarning("[DuplicateComponentKiller] No target component set and none auto-detected. Doing nothing.");
                return;
            }
            targetComponent = first;
        }

        var t = targetComponent.GetType();
        var all = FindObjectsOfType(t, true).Cast<Component>().ToArray();
        if (all.Length <= 1) return;

        // Vælg overlever tidligt
        Component survivor = all[0];
        switch (keep)
        {
            case KeepStrategy.Newest:
                survivor = all[all.Length - 1];
                break;
            case KeepStrategy.InActiveScene:
                var activeScene = gameObject.scene;
                var inActive = all.Where(c => c != null && c.gameObject.scene == activeScene).ToArray();
                survivor = inActive.FirstOrDefault() ?? all[0];
                break;
            case KeepStrategy.Oldest:
            default:
                survivor = all[0];
                break;
        }

        // Slå alle andre ihjel STRAKS, så de ikke når at initialisere noget som helst
        foreach (var c in all)
        {
            if (c == null || c == survivor) continue;
            var go = c.gameObject;
            Debug.LogWarning($"[DuplicateComponentKiller] DestroyImmediate duplicate {t.Name} on '{GetPath(go.transform)}' (scene='{go.scene.name}'). Survivor in scene '{survivor.gameObject.scene.name}'.");
#if UNITY_EDITOR
            // I play mode vil vi stadig have en øjeblikkelig fjernelse
            if (Application.isPlaying)
                Object.DestroyImmediate(go);
            else
                Object.DestroyImmediate(go);
#else
            Object.DestroyImmediate(go);
#endif
        }
    }

    private static string GetPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null) { t = t.parent; path = t.name + "/" + path; }
        return path;
    }
}
