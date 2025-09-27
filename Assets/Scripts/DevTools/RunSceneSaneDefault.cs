using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using System.Linq;

/// <summary>
/// Minst indgribende run-guard:
/// - Garanterer sane defaults ved run-start uden at ændre dine egne systemer.
/// - Logger (og kan valgfrit deaktivere) overlay-canvases der blokerer input.
/// - Sikrer EventSystem + UI input.
/// - Logger duplicate singletons, hvis du angiver deres navne i inspector.
/// </summary>
public class RunSceneSaneDefaults : MonoBehaviour
{
    [Header("Safety")]
    [Tooltip("Sæt til true for at tvinge Time.timeScale = 1 ved scene start.")]
    public bool forceTimeScaleOne = true;

    [Tooltip("Hvis true, deaktiverer vi automatisk overlay CanvasGroups som blokerer raycasts (fx glemte modaler).")]
    public bool autoDisableBlockingOverlays = true;

    [Header("Diagnostics")]
    [Tooltip("Navne på GameObjects eller komponent-typer du vil tjekke for dobbelt-instans (DontDestroyOnLoad problemer).")]
    public string[] duplicateSentries = new string[]
    {
        "CardHandUI",
        "DeckManager",
        "CardRewardUI",
        "Wallet",
        "PlayerBuffManager",
        "SkillTreeManager"
    };

    void Awake()
    {
        // 1) Timescale sanity
        if (forceTimeScaleOne && Time.timeScale != 1f)
        {
            Debug.LogWarning($"[RunSceneSaneDefaults] Time.timeScale var {Time.timeScale}. Sætter til 1.");
            Time.timeScale = 1f;
        }

        // 2) EventSystem + UI input
        EnsureEventSystemAndUIInput();

        // 3) Find (og evt. slå fra) overlay canvases som blokerer alt input
        HandleBlockingOverlays();

        // 4) Hurtig check for dubletter (navne/komponenter du angiver)
        CheckDuplicateSentries();
    }

    private void EnsureEventSystemAndUIInput()
    {
        var es = FindObjectOfType<EventSystem>();
        if (!es)
        {
            var go = new GameObject("EventSystem");
            es = go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>(); // fallback hvis du ikke bruger Input System UI
            Debug.LogWarning("[RunSceneSaneDefaults] Manglede EventSystem. Oprettet nyt.");
        }

#if ENABLE_INPUT_SYSTEM
        var uiInput = es.GetComponent<InputSystemUIInputModule>();
        if (!uiInput)
        {
            uiInput = es.gameObject.AddComponent<InputSystemUIInputModule>();
            Debug.LogWarning("[RunSceneSaneDefaults] Manglede InputSystemUIInputModule. Oprettet.");
        }
        uiInput.enabled = true;
#endif
    }

    private void HandleBlockingOverlays()
    {
        var allCanvasGroups = FindObjectsOfType<CanvasGroup>(true);
        var blockers = allCanvasGroups
            .Where(cg => cg.gameObject.activeInHierarchy && (cg.blocksRaycasts || cg.interactable) && cg.alpha > 0.01f)
            .ToList();

        if (blockers.Count == 0) return;

        // Filtrer sandsynlige syndere (typisk modaler/menus udenfor run UI)
        var likelyBad =
            blockers.Where(cg =>
                cg.name.ToLower().Contains("skill") ||
                cg.name.ToLower().Contains("tree") ||
                cg.name.ToLower().Contains("reward") ||
                cg.name.ToLower().Contains("dimmer") ||
                cg.name.ToLower().Contains("modal") ||
                cg.name.ToLower().Contains("menu"))
            .ToList();

        if (likelyBad.Count > 0)
        {
            foreach (var cg in likelyBad)
            {
                Debug.LogWarning($"[RunSceneSaneDefaults] Overlay blokerer input: {GetPath(cg.transform)}  (blocksRaycasts={cg.blocksRaycasts}, interactable={cg.interactable}, alpha={cg.alpha})");

                if (autoDisableBlockingOverlays)
                {
                    cg.gameObject.SetActive(false);
                    Debug.Log($"[RunSceneSaneDefaults] Deaktiveret '{GetPath(cg.transform)}'.");
                }
            }
        }
        else
        {
            // Ingen "åbenlyse" – log alt, så du kan se hvad der fanger input
            foreach (var cg in blockers)
            {
                Debug.Log($"[RunSceneSaneDefaults] Aktiv CanvasGroup: {GetPath(cg.transform)}  (blocksRaycasts={cg.blocksRaycasts}, interactable={cg.interactable}, alpha={cg.alpha})");
            }
        }
    }

    private void CheckDuplicateSentries()
    {
        if (duplicateSentries == null || duplicateSentries.Length == 0) return;

        foreach (var key in duplicateSentries)
        {
            if (string.IsNullOrWhiteSpace(key)) continue;

            // Tjek efter GameObjects med navnet
            var gos = Resources.FindObjectsOfTypeAll<GameObject>()
                .Where(go => go.name == key)
                .ToArray();

            if (gos.Length > 1)
            {
                Debug.LogWarning($"[RunSceneSaneDefaults] Mulig duplicate af '{key}': {gos.Length} objekter (DontDestroyOnLoad?). Første: {GetPath(gos[0].transform)}");
            }

            // Tjek efter komponent-type ved navn
            var type = System.AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.Name == key && typeof(Component).IsAssignableFrom(t));

            if (type != null)
            {
                var comps = FindObjectsOfType(type);
                if (comps != null && comps.Length > 1)
                {
                    Debug.LogWarning($"[RunSceneSaneDefaults] Mulig duplicate komponent '{key}': {comps.Length} instanser.");
                }
            }
        }
    }

    private static string GetPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null) { t = t.parent; path = t.name + "/" + path; }
        return path;
    }
}
