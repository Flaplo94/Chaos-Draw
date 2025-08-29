// DEPRECATED - candidate for deletion/merge
#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ProjectCleanupAudit : EditorWindow
{
    [MenuItem("Tools/Chaos Draw/Project Audit")]
    public static void Open() => GetWindow<ProjectCleanupAudit>("Project Audit");

    // Heuristics (adjust as needed)
    static readonly string[] CalculatorHints = { "ComputeFinalDamage", "DamageCalculator", "GetGenericDamageMult", "GetFireDamageMult", "GetLightningDamageMult", "GetBurnDamageMult" };
    static readonly string[] ApplierHints    = { "Damage.ApplyTo(", "ApplyDamage(", "DealDamage(", "OnTriggerEnter2D", "OnCollisionEnter2D" };
    static readonly string[] HealthHints     = { "TakeDamage(", "class Health", "EnemyHealth", "BossHealth" };
    static readonly string[] AbilityHints    = { "IAbilityBehavior", "class Ability", "effectPrefab", "Spawn", "Instantiate(", "DoT", "cooldown" };

    class ScriptInfo
    {
        public string guid;
        public string path;
        public string text;
        public string className;
        public string @namespace;
        public bool isMono;
        public bool isEditor;
        public bool referenced;                   // used by scenes/prefabs
        public bool hasSerializedFields;
        public bool hasEmptyUpdate;
        public bool isCalculator, isApplier, isHealth, isAbilityish;
        public string suggestedFolder;            // suggested target folder
    }

    Vector2 scroll;
    List<UnityEngine.Object> refAssets = new();
    List<ScriptInfo> all = new();
    Dictionary<string, List<ScriptInfo>> byClass = new();

    // Folder suggestions (simple keyword regex -> target folder)
    static readonly (Regex rx, string folder)[] FolderRules = new (Regex, string)[] {
        (new Regex(@"\bDamage(Element|Calculator|\.ApplyTo)\b"), "Assets/Scripts/Systems/Combat"),
        (new Regex(@"\b(PlayerBuffManager|BuffData|ArtifactData)\b"), "Assets/Scripts/Systems/Buffs"),
        (new Regex(@"\b(Shop(Item|Catalog|Manager)|RemoveCard)\b"), "Assets/Scripts/Systems/Shop"),
        (new Regex(@"\b(WaveManager|EnemySpawner)\b"), "Assets/Scripts/Systems/Waves"),
        (new Regex(@"\b(TopBar|ShopItemUI|Modal|TMP|UI)\b"), "Assets/Scripts/UI"),
        (new Regex(@"\b(PlayerMovement|PlayerShooting|PlayerInventory|IAbilityBehavior|Ability|Fire|Dash|Shield)\b"), "Assets/Scripts/Player"),
        (new Regex(@"\b(EnemyHealth|BossHealth|Enemy)\b"), "Assets/Scripts/Enemies"),
        (new Regex(@"\b(Singleton|GameEvents|Bootstrap|Service|Util|Extensions)\b"), "Assets/Scripts/Core"),
    };

    void OnGUI()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Scan Project", GUILayout.Height(26))) Scan();
            if (all.Count > 0 && GUILayout.Button("Refresh", GUILayout.Height(26))) Scan();
        }

        if (all.Count == 0)
        {
            EditorGUILayout.HelpBox("Click 'Scan Project' to run a full audit.", MessageType.Info);
            return;
        }

        EditorGUILayout.Space(6);
        DrawSummary();

        scroll = EditorGUILayout.BeginScrollView(scroll);

        DrawGroup("Unused scripts (not referenced by any Prefab/Scene)", all.Where(s => !s.referenced).ToList(), new Color(1f, .75f, .75f));
        DrawGroup("Duplicate class names", byClass.Where(kv => kv.Value.Count > 1).SelectMany(kv => kv.Value).ToList(), new Color(1f, .85f, .65f));
        DrawGroup("MonoBehaviours without serialized fields", all.Where(s => s.isMono && !s.hasSerializedFields).ToList(), new Color(.95f, .95f, 1f));
        DrawGroup("Empty Update/FixedUpdate/LateUpdate", all.Where(s => s.hasEmptyUpdate).ToList(), new Color(.9f, 1f, .9f));

        EditorGUILayout.Space(8);
        DrawGroup("Calculators (keep one)", all.Where(s => s.isCalculator).ToList(), new Color(.7f, 1f, 1f));
        DrawGroup("Appliers (consolidate to Damage.cs)", all.Where(s => s.isApplier && !s.isCalculator && !s.isHealth).ToList(), new Color(1f, .92f, .7f));
        DrawGroup("Health (Enemy/Boss/Health)", all.Where(s => s.isHealth).ToList(), new Color(.8f, 1f, .8f));
        DrawGroup("Ability/Projectile/DoT (should call Damage.ApplyTo only)", all.Where(s => s.isAbilityish && !s.isCalculator && !s.isHealth).ToList(), new Color(.9f, .9f, 1f));

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(8);
        EditorGUILayout.HelpBox(
@"Recommended workflow:
1) Mark unused scripts as _OLD (safe to delete later).
2) Resolve duplicate class names (rename or merge).
3) Remove empty Update/Fixed/Late methods or refactor to events.
4) Move scripts into suggested folders (Move button).
5) Consolidate appliers into Damage.cs and keep a single DamageCalculator.", MessageType.None);
    }

    void DrawSummary()
    {
        int unused = all.Count(s => !s.referenced);
        int duplicates = byClass.Count(kv => kv.Value.Count > 1);
        int emptyUpdates = all.Count(s => s.hasEmptyUpdate);
        int editorOnly = all.Count(s => s.isEditor);

        EditorGUILayout.LabelField($"Scripts total: {all.Count}  |  Unused: {unused}  |  Duplicate classes: {duplicates}  |  Empty Updates: {emptyUpdates}  |  Editor scripts: {editorOnly}");
    }

    void DrawGroup(string title, List<ScriptInfo> list, Color bg)
    {
        if (list.Count == 0) return;

        using (new EditorGUILayout.VerticalScope("box"))
        {
            var old = GUI.backgroundColor;
            GUI.backgroundColor = bg;
            EditorGUILayout.LabelField($"{title}  ({list.Count})", EditorStyles.boldLabel);
            GUI.backgroundColor = old;

            foreach (var s in list)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("-", GUILayout.Width(12));
                    GUILayout.Label(s.className, GUILayout.Width(220));
                    GUILayout.Label(s.referenced ? "REF" : "UNUSED", GUILayout.Width(66));
                    GUILayout.Label(s.suggestedFolder ?? "-", GUILayout.Width(260));
                    GUILayout.Label(s.path);

                    if (GUILayout.Button("Ping", GUILayout.Width(48)))
                        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(s.path));

                    if (GUILayout.Button("Open", GUILayout.Width(48)))
                        AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(s.path));

                    if (GUILayout.Button("_OLD", GUILayout.Width(58)))
                        MarkDeprecated(s);

                    if (!string.IsNullOrEmpty(s.suggestedFolder) && s.suggestedFolder != Path.GetDirectoryName(s.path).Replace("\\","/"))
                    {
                        if (GUILayout.Button("Move", GUILayout.Width(52)))
                            MoveToFolder(s, s.suggestedFolder);
                    }
                }
            }
        }
    }

    void Scan()
    {
        all.Clear();
        byClass.Clear();
        refAssets.Clear();

        // Cache prefabs/scenes for reference scan (script GUID lookups in YAML)
        foreach (var g in AssetDatabase.FindAssets("t:Prefab t:Scene", new[] { "Assets" }))
        {
            var p = AssetDatabase.GUIDToAssetPath(g);
            var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(p);
            if (obj != null) refAssets.Add(obj);
        }

        // Find all .cs
        var csGuids = AssetDatabase.FindAssets("t:TextAsset", new[] { "Assets" })
            .Where(g => AssetDatabase.GUIDToAssetPath(g).EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        foreach (var guid in csGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            string text;
            try { text = File.ReadAllText(path); }
            catch { continue; }

            var info = new ScriptInfo
            {
                guid = guid,
                path = path,
                text = text,
                className = GuessClassName(path, text),
                @namespace = GuessNamespace(text),
                isMono = Regex.IsMatch(text, @"\bclass\s+\w+\s*:\s*MonoBehaviour\b"),
                isEditor = path.Contains("/Editor/"),
                isCalculator = ContainsAny(text, CalculatorHints),
                isApplier    = ContainsAny(text, ApplierHints),
                isHealth     = ContainsAny(text, HealthHints),
                isAbilityish = ContainsAny(text, AbilityHints),
                referenced = IsScriptReferencedAnywhere(guid),
                hasSerializedFields = Regex.IsMatch(text, @"\[\s*SerializeField\s*\]|\bpublic\s+(int|float|bool|string|Vector2|Vector3|Color|Sprite|GameObject|Transform|Rigidbody2D|Animator|AudioClip|ScriptableObject|MonoBehaviour)\b"),
                hasEmptyUpdate = HasEmptyUpdate(text),
                suggestedFolder = SuggestFolder(text)
            };

            all.Add(info);
            if (!byClass.ContainsKey(info.className)) byClass[info.className] = new List<ScriptInfo>();
            byClass[info.className].Add(info);
        }

        // Sort: referenced first
        all = all.OrderByDescending(s => s.referenced).ThenBy(s => s.path).ToList();
        Repaint();
    }

    static string GuessClassName(string path, string text)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        var m = Regex.Match(text, @"\bclass\s+([A-Za-z_]\w*)");
        if (m.Success) name = m.Groups[1].Value;
        return name;
    }

    static string GuessNamespace(string text)
    {
        var m = Regex.Match(text, @"\bnamespace\s+([A-Za-z0-9_.]+)");
        return m.Success ? m.Groups[1].Value : "";
    }

    bool IsScriptReferencedAnywhere(string scriptGuid)
    {
        foreach (var obj in refAssets)
        {
            var p = AssetDatabase.GetAssetPath(obj);
            try
            {
                var t = File.ReadAllText(p);
                if (t.Contains(scriptGuid)) return true;
            }
            catch { }
        }
        return false;
    }

    static bool ContainsAny(string hay, string[] needles)
    {
        foreach (var n in needles)
            if (hay.IndexOf(n, StringComparison.Ordinal) >= 0)
                return true;
        return false;
    }

    static bool HasEmptyUpdate(string text)
    {
        // matches empty Update/Fixed/Late: void Update(){ } or only whitespace inside
        return Regex.IsMatch(text, @"void\s+(Update|FixedUpdate|LateUpdate)\s*\(\s*\)\s*\{\s*\}");
    }

    static string SuggestFolder(string text)
    {
        foreach (var (rx, folder) in FolderRules)
            if (rx.IsMatch(text)) return folder;
        return null;
    }

    void MarkDeprecated(ScriptInfo s)
    {
        try
        {
            var contents = File.ReadAllText(s.path);
            if (!contents.StartsWith("// DEPRECATED", StringComparison.Ordinal))
                File.WriteAllText(s.path, "// DEPRECATED - candidate for deletion/merge\n" + contents);

            var dir = Path.GetDirectoryName(s.path)?.Replace("\\","/");
            var name = Path.GetFileNameWithoutExtension(s.path);
            var newPath = $"{dir}/{name}_OLD.cs";
            if (!File.Exists(newPath))
            {
                AssetDatabase.StartAssetEditing();
                try { AssetDatabase.MoveAsset(s.path, newPath); }
                finally { AssetDatabase.StopAssetEditing(); }
            }
            AssetDatabase.Refresh();
        }
        catch (Exception e)
        {
            Debug.LogError($"Could not mark _OLD: {s.path}\n{e}");
        }
    }

    void MoveToFolder(ScriptInfo s, string targetFolder)
    {
        try
        {
            if (!AssetDatabase.IsValidFolder(targetFolder))
            {
                var parts = targetFolder.Split('/');
                string current = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    string next = current + "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(next))
                        AssetDatabase.CreateFolder(current, parts[i]);
                    current = next;
                }
            }
            var fileName = Path.GetFileName(s.path);
            var dest = $"{targetFolder}/{fileName}";
            AssetDatabase.MoveAsset(s.path, dest);
            AssetDatabase.Refresh();
        }
        catch (Exception e)
        {
            Debug.LogError($"Could not move: {s.path}  {targetFolder}\n{e}");
        }
    }
}
#endif
