using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class DebugWaveSkipper : MonoBehaviour
{
    [Header("UI (optional)")]
    public Button completeWaveButton;
    public Canvas targetCanvas;
    public bool createButtonIfMissing = true;
    public string buttonText = "Complete Wave";

    [Header("Hotkey")]
    public KeyCode hotkey = KeyCode.F9;

    [Header("Kill Settings")]
    public bool killBosses = true;
    public bool destroyIfNoKillMethod = false;
    public float megaDamage = 999999f;

    [Header("Spawner Handling (best effort)")]
    public bool tryStopSpawners = true;

    [Header("Optional unlock assist (dev only)")]
    [Tooltip("Hvis du tester ved at cleare wave >= unlockWaveIndex, kan vi (valgfrit) sætte permanent skill tree unlock.")]
    public bool callUnlockSkillTreeIfWave10 = false;

    [Tooltip("Wave-indeks/nummer der udløser permanent unlock i test (brug 10 for din boss-wave).")]
    public int unlockWaveIndex = 10;

    void Start()
    {
        TryWireButton();
    }

    void Update()
    {
        if (Input.GetKeyDown(hotkey))
            StartCoroutine(CompleteCurrentWave());
    }

    private void TryWireButton()
    {
        if (completeWaveButton != null)
        {
            completeWaveButton.onClick.RemoveAllListeners();
            completeWaveButton.onClick.AddListener(() => StartCoroutine(CompleteCurrentWave()));
            return;
        }

        if (!createButtonIfMissing) return;

        if (targetCanvas == null) targetCanvas = UnityFindFirst<Canvas>();
        if (targetCanvas == null) return;

        var go = new GameObject("DEV_CompleteWave_Button",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(targetCanvas.transform, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-20f, -20f);
        rt.sizeDelta = new Vector2(160f, 36f);

        var img = go.GetComponent<Image>();
        img.raycastTarget = true;

        var btn = go.GetComponent<Button>();
        btn.onClick.AddListener(() => StartCoroutine(CompleteCurrentWave()));

        var textGO = new GameObject("Text", typeof(RectTransform));
        textGO.transform.SetParent(go.transform, false);
        var trt = textGO.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;

        var txt = textGO.AddComponent<UnityEngine.UI.Text>();
        txt.text = string.IsNullOrEmpty(buttonText) ? "Complete Wave" : buttonText;
        txt.alignment = TextAnchor.MiddleCenter;

        // *** ÆNDRING: Arial.ttf -> LegacyRuntime.ttf (med fallback) ***
        Font builtin = null;
        try { builtin = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
        if (builtin != null)
            txt.font = builtin;
        // hvis ikke fundet, lader vi font stå som default/inspector

        txt.resizeTextForBestFit = true;
        txt.resizeTextMinSize = 10;
        txt.resizeTextMaxSize = 28;
        txt.color = Color.black;

        completeWaveButton = btn;
    }

    private IEnumerator CompleteCurrentWave()
    {
        Debug.Log("[DebugWaveSkipper] Completing current wave...");

        if (tryStopSpawners) TryStopAllSpawners();

        int killedEnemies = KillAllByTypeName("EnemyHealth");
        int killedBosses = killBosses ? KillAllByTypeName("BossHealth") : 0;
        killedEnemies += KillAllTagged("Enemy");

        // Lad OnDeath events køre
        yield return null;
        yield return null;

        Debug.Log($"[DebugWaveSkipper] Requested kill: total ~ {killedEnemies + killedBosses}");

        if (callUnlockSkillTreeIfWave10)
        {
            int? wave = TryGetCurrentWaveIndex();
            if (wave.HasValue && wave.Value >= unlockWaveIndex)
                TryUnlockSkillTreePermanent();
        }
    }

    // -------------------- Kill helpers --------------------
    private int KillAllByTypeName(string typeName)
    {
        int count = 0;
        var allMB = UnityFindAll<MonoBehaviour>(false);
        for (int i = 0; i < allMB.Length; i++)
        {
            var mb = allMB[i];
            if (mb == null) continue;
            if (mb.GetType().Name != typeName) continue;
            if (TryKillGameObject(mb.gameObject)) count++;
        }
        return count;
    }

    private int KillAllTagged(string tag)
    {
        int count = 0;
        GameObject[] tagged = null;
        try { tagged = GameObject.FindGameObjectsWithTag(tag); } catch { return 0; }
        if (tagged == null || tagged.Length == 0) return 0;

        for (int i = 0; i < tagged.Length; i++)
        {
            var go = tagged[i];
            if (go != null && TryKillGameObject(go)) count++;
        }
        return count;
    }

    private bool TryKillGameObject(GameObject go)
    {
        if (go == null) return false;

        // Special-case: EnemyHealth / BossHealth — prøv *den rigtige* TakeDamage-signatur (int, DamageElement)
        var enemyHealth = go.GetComponent("EnemyHealth");
        var bossHealth = go.GetComponent("BossHealth");
        if (enemyHealth != null || bossHealth != null)
        {
            var comp = (enemyHealth != null) ? enemyHealth : bossHealth;
            if (TryInvokeTakeDamageWithElement(comp, (int)megaDamage))
                return true;
        }

        var comps = go.GetComponents<MonoBehaviour>();
        for (int i = 0; i < comps.Length; i++)
        {
            var c = comps[i];
            if (c == null) continue;

            // Almindelige dødskald
            if (InvokeZero(c, "Kill")) return true;
            if (InvokeZero(c, "Die")) return true;
            if (InvokeZero(c, "HandleDeath")) return true;
            if (InvokeZero(c, "OnDeath")) return true;

            // Generiske damage-varianter
            if (InvokeDamage(c, "TakeDamage")) return true;
            if (InvokeDamage(c, "ApplyDamage")) return true;
            if (InvokeDamage(c, "ReceiveDamage")) return true;

            // Fald tilbage: sæt HP=0 + prøv dødskald
            if (TrySetHealthToZero(c))
            {
                if (InvokeZero(c, "OnDeath")) return true;
                if (InvokeZero(c, "Die")) return true;
                if (InvokeZero(c, "HandleDeath")) return true;

                // Nogle fjender destruerer i animation-event:
                try { go.SendMessage("FinishDeath", SendMessageOptions.DontRequireReceiver); } catch { }

                if (destroyIfNoKillMethod)
                {
                    Destroy(go);
                    return true;
                }
                return true;
            }
        }

        if (destroyIfNoKillMethod)
        {
            Destroy(go);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Kalder specifikt TakeDamage(int, DamageElement) via reflection.
    /// Finder enum-typen "DamageElement" og bruger første værdi (eller 0).
    /// </summary>
    private bool TryInvokeTakeDamageWithElement(object component, int dmg)
    {
        if (component == null) return false;

        var t = component.GetType();
        // Find enum-typen "DamageElement" i samme assembly
        var asm = t.Assembly;
        Type elemEnum = null;

        foreach (var tt in asm.GetTypes())
        {
            if (!tt.IsEnum) continue;
            if (tt.Name == "DamageElement") { elemEnum = tt; break; }
        }

        if (elemEnum == null)
        {
            // Prøv at finde den i AppDomain (hvis den ligger i en anden assembly)
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var tt in a.GetTypes())
                {
                    if (tt.IsEnum && tt.Name == "DamageElement") { elemEnum = tt; break; }
                }
                if (elemEnum != null) break;
            }
        }

        if (elemEnum == null) return false;

        // Vælg en gyldig enumværdi (første eller 0)
        Array values = Enum.GetValues(elemEnum);
        object elemValue = (values != null && values.Length > 0) ? values.GetValue(0) : Enum.ToObject(elemEnum, 0);

        // Find overload: TakeDamage(int, DamageElement)
        var m = t.GetMethod("TakeDamage",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null, new Type[] { typeof(int), elemEnum }, null);

        if (m == null) return false;

        try
        {
            m.Invoke(component, new object[] { dmg, elemValue });
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[DebugWaveSkipper] TakeDamage(int,DamageElement) failed on {t.Name}: {e.Message}");
            return false;
        }
    }

    private bool InvokeZero(object target, string methodName)
    {
        var t = target.GetType();
        var m = t.GetMethod(methodName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null, Type.EmptyTypes, null);
        if (m == null) return false;
        try { m.Invoke(target, null); return true; } catch { return false; }
    }

    private bool InvokeDamage(object target, string methodName)
    {
        var t = target.GetType();

        var m1 = t.GetMethod(methodName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null, new Type[] { typeof(float) }, null);
        if (m1 != null) { try { m1.Invoke(target, new object[] { megaDamage }); return true; } catch { } }

        var m2 = t.GetMethod(methodName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null, new Type[] { typeof(int) }, null);
        if (m2 != null) { try { m2.Invoke(target, new object[] { (int)megaDamage }); return true; } catch { } }

        var m3 = t.GetMethod(methodName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null, new Type[] { typeof(float), typeof(object) }, null);
        if (m3 != null) { try { m3.Invoke(target, new object[] { megaDamage, null }); return true; } catch { } }

        var m4 = t.GetMethod(methodName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null, new Type[] { typeof(int), typeof(object) }, null);
        if (m4 != null) { try { m4.Invoke(target, new object[] { (int)megaDamage, null }); return true; } catch { } }

        return false;
    }

    private bool TrySetHealthToZero(object target)
    {
        var t = target.GetType();

        string[] fieldNames = { "hp", "health", "currentHP", "currentHealth", "Health", "HP" };
        for (int i = 0; i < fieldNames.Length; i++)
        {
            var f = t.GetField(fieldNames[i], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f == null) continue;
            try
            {
                if (f.FieldType == typeof(int)) { f.SetValue(target, 0); return true; }
                if (f.FieldType == typeof(float)) { f.SetValue(target, 0f); return true; }
            }
            catch { }
        }

        string[] propNames = { "HP", "Health", "CurrentHP", "CurrentHealth" };
        for (int i = 0; i < propNames.Length; i++)
        {
            var p = t.GetProperty(propNames[i], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p == null || !p.CanWrite) continue;
            try
            {
                if (p.PropertyType == typeof(int)) { p.SetValue(target, 0); return true; }
                if (p.PropertyType == typeof(float)) { p.SetValue(target, 0f); return true; }
            }
            catch { }
        }

        return false;
    }

    private void TryStopAllSpawners()
    {
        var allMB = UnityFindAll<MonoBehaviour>(false);
        for (int i = 0; i < allMB.Length; i++)
        {
            var mb = allMB[i];
            if (mb == null) continue;
            var tn = mb.GetType().Name;
            if (tn.Contains("Spawner"))
            {
                InvokeZero(mb, "StopSpawning");
                InvokeZero(mb, "Stop");
                InvokeZero(mb, "ForceEndWave");
            }
        }
    }

    // -------------------- Wave index (reflection, no compile dependency) --------------------
    private int? TryGetCurrentWaveIndex()
    {
        var waveMgr = UnityFindFirst<WaveManager>();
        if (waveMgr == null)
        {
            var all = UnityFindAll<MonoBehaviour>(false);
            for (int i = 0; i < all.Length; i++)
            {
                var mb = all[i];
                if (mb != null && mb.GetType().Name.Contains("WaveManager"))
                { waveMgr = mb as WaveManager; break; }
            }
        }
        if (waveMgr == null) return null;

        object obj = waveMgr;
        Type t = obj.GetType();

        string[] fieldNames = { "CurrentWaveIndex", "currentWaveIndex", "currentWave", "waveIndex", "wave", "currentWaveNumber", "waveNumber" };
        for (int i = 0; i < fieldNames.Length; i++)
        {
            var f = t.GetField(fieldNames[i], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null && f.FieldType == typeof(int))
            { try { return (int)f.GetValue(obj); } catch { } }
        }

        string[] propNames = { "CurrentWaveIndex", "CurrentWave", "WaveIndex", "Wave", "CurrentWaveNumber", "WaveNumber" };
        for (int i = 0; i < propNames.Length; i++)
        {
            var p = t.GetProperty(propNames[i], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p != null && p.CanRead && p.PropertyType == typeof(int))
            { try { return (int)p.GetValue(obj, null); } catch { } }
        }

        string[] methodNames = { "GetCurrentWaveIndex", "GetWaveIndex", "GetCurrentWave", "GetWaveNumber" };
        for (int i = 0; i < methodNames.Length; i++)
        {
            var m = t.GetMethod(methodNames[i], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
            if (m != null && m.ReturnType == typeof(int))
            { try { return (int)m.Invoke(obj, null); } catch { } }
        }

        return null;
    }

    // -------------------- Meta unlock (reflection, no compile dependency) --------------------
    private void TryUnlockSkillTreePermanent()
    {
        var meta = UnityFindFirst<MetaProgressionManager>();
        if (meta == null) return;

        var t = meta.GetType();

        var m = t.GetMethod("UnlockSkillTreePermanent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (m != null) { try { m.Invoke(meta, null); return; } catch { } }

        var f = t.GetField("skillTreeUnlocked", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (f != null && f.FieldType == typeof(bool)) { try { f.SetValue(meta, true); } catch { } }

        var p = t.GetProperty("skillTreeUnlocked", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (p != null && p.CanWrite && p.PropertyType == typeof(bool)) { try { p.SetValue(meta, true); } catch { } }

        var save = t.GetMethod("SaveData", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (save != null) { try { save.Invoke(meta, null); } catch { } }

        Debug.Log("[DebugWaveSkipper] Skill tree unlocked (fallback).");
    }

    // -------------------- Unity version compatibility helpers --------------------
    private static T UnityFindFirst<T>() where T : UnityEngine.Object
    {
        var obj = UnityEngine.Object.FindObjectOfType<T>();
        if (obj != null) return obj;
        var all = Resources.FindObjectsOfTypeAll<T>();
        return all != null && all.Length > 0 ? all[0] : null;
    }

    private static T[] UnityFindAll<T>(bool includeInactive) where T : UnityEngine.Object
    {
        if (!includeInactive)
            return UnityEngine.Object.FindObjectsOfType<T>();
        return Resources.FindObjectsOfTypeAll<T>();
    }
}
