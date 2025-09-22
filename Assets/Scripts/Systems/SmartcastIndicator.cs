using System;
using System.Reflection;
using UnityEngine;
using TMPro;

public class SmartcastIndicator : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private LineRenderer line;
    [SerializeField] private TMP_Text nameLabel;

    [Header("Name Label")]
    [SerializeField] private Vector2 nameScreenOffset = new Vector2(0f, -32f);

    [Header("Line Settings")]
    [SerializeField] private float lineWidth = 0.05f;
    [SerializeField] private Material fallbackMaterial; // optional; assign Sprites/Default if you like

    [Header("Per-Ability Overrides")]
    [SerializeField] private PreviewOverride[] overrides; // set these in Inspector
    [Header("Behavior")]
    [SerializeField] private bool lineAlwaysFullRange = true;
    public enum PreviewType
    {
        LineFromPlayer,   // e.g. Fireball
        CircleOnPlayer,   // e.g. Chain Lightning reach
        CircleOnMouse,    // e.g. Fire on Ground AOE
        ConeFromPlayer
    }

    [Serializable]
    public struct PreviewOverride
    {
        [Tooltip("Match by internalID if available; if empty or not found, falls back to name substring match.")]
        public string idOrName;
        [Tooltip("How to draw this ability.")]
        public PreviewType type;

        [Tooltip("Set a custom range (LineFromPlayer, CircleOnPlayer, ConeFromPlayer).")]
        public bool setRange;
        public float range;

        [Tooltip("Set a custom AOE radius (CircleOnMouse).")]
        public bool setAoeRadius;
        public float aoeRadius;

        [Tooltip("Set a custom cone angle (ConeFromPlayer).")]
        public bool setConeAngle;
        public float coneAngle;
    }

    private bool previewShown = false;
    private PreviewType type = PreviewType.CircleOnMouse;
    private float range = 6f;     // used by LineFromPlayer, CircleOnPlayer, ConeFromPlayer
    private float aoeRadius = 2f; // used by CircleOnMouse
    private float coneAngle = 60f;
    private Transform player;

    public bool IsPreviewShown => previewShown;

    void Awake()
    {
        var pm = FindFirstObjectByType<PlayerMovement>();
        if (pm != null) player = pm.transform;

        if (line != null)
        {
            line.useWorldSpace = true;
            line.loop = false;
            line.startWidth = lineWidth;
            line.endWidth = lineWidth;

            if (line.sharedMaterial == null)
            {
                if (fallbackMaterial != null) line.material = fallbackMaterial;
                else
                {
                    var shader = Shader.Find("Sprites/Default");
                    if (shader != null) line.material = new Material(shader);
                }
            }
            line.enabled = false;
            line.positionCount = 0;
        }

        if (nameLabel != null) nameLabel.raycastTarget = false;
    }

    // -------- Name label control --------
    public void SetNameText(string text)
    {
        if (nameLabel != null) nameLabel.text = text;
    }

    public void SetNameVisible(bool visible)
    {
        if (nameLabel != null) nameLabel.gameObject.SetActive(visible);
    }

    public void UpdateNamePosition(Vector2 mouseWorld)
    {
        if (nameLabel == null || !nameLabel.gameObject.activeSelf) return;
        var cam = Camera.main;
        Vector3 screen = cam != null
            ? cam.WorldToScreenPoint(new Vector3(mouseWorld.x, mouseWorld.y, 0f))
            : new Vector3(mouseWorld.x, mouseWorld.y, 0f);
        nameLabel.transform.position = screen + (Vector3)nameScreenOffset;
    }

    // -------- Preview control --------
    public void ShowPreviewFor(Ability a)
    {
        ResolvePreviewFromAbility(a);
        previewShown = true;
        if (line != null) { line.enabled = true; line.positionCount = 0; }
    }

    public void UpdatePreviewAim(Vector2 mouseWorld)
    {
        if (!previewShown || line == null) return;

        switch (type)
        {
            case PreviewType.LineFromPlayer:
                DrawAimLineFromPlayer(mouseWorld, Mathf.Max(0.01f, range), lineAlwaysFullRange);
                break;
            case PreviewType.CircleOnPlayer:
                DrawCircle(GetPlayerPos(), Mathf.Max(0.01f, range));
                break;
            case PreviewType.CircleOnMouse:
                DrawCircle(mouseWorld, Mathf.Max(0.01f, aoeRadius));
                break;
            case PreviewType.ConeFromPlayer:
                DrawCone(GetPlayerPos(), mouseWorld, Mathf.Clamp(coneAngle, 5f, 179f), Mathf.Max(0.01f, range));
                break;
        }
    }

    public void HidePreview()
    {
        previewShown = false;
        if (line != null) { line.enabled = false; line.positionCount = 0; }
    }

    // -------- Drawing --------
    private void DrawAimLineFromPlayer(Vector2 mouseWorld, float maxLen, bool alwaysFullLen = true)
    {
        Vector2 origin = GetPlayerPos();
        Vector2 v = mouseWorld - origin;
        float lenToMouse = v.magnitude;

        // direction toward mouse (fallback if the mouse is exactly on the player)
        Vector2 dir = (lenToMouse > 0.0001f) ? (v / lenToMouse) : Vector2.right;

        // always draw full length if requested
        float used = alwaysFullLen ? maxLen : Mathf.Min(maxLen, lenToMouse);

        Vector2 end = origin + dir * used;

        line.positionCount = 2;
        line.SetPosition(0, new Vector3(origin.x, origin.y, 0f));
        line.SetPosition(1, new Vector3(end.x, end.y, 0f));
    }


    private void DrawCircle(Vector2 center, float radius, int segments = 64)
    {
        line.loop = true;
        line.positionCount = segments;
        float step = (Mathf.PI * 2f) / (segments - 1);
        for (int i = 0; i < segments; i++)
        {
            float ang = i * step;
            float x = Mathf.Cos(ang) * radius + center.x;
            float y = Mathf.Sin(ang) * radius + center.y;
            line.SetPosition(i, new Vector3(x, y, 0f));
        }
        line.loop = false;
    }

    private void DrawCone(Vector2 origin, Vector2 mouseWorld, float angleDeg, float length, int segments = 32)
    {
        Vector2 dir = (mouseWorld - origin);
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right; else dir.Normalize();

        float half = angleDeg * 0.5f;
        Quaternion qL = Quaternion.Euler(0, 0, -half);
        Quaternion qR = Quaternion.Euler(0, 0, half);

        Vector2 left = qL * dir;
        Vector2 right = qR * dir;

        line.positionCount = segments + 3;
        line.SetPosition(0, new Vector3(origin.x, origin.y, 0f));
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            Vector2 edgeDir = Vector2.Lerp(left, right, t).normalized;
            Vector2 p = origin + edgeDir * length;
            line.SetPosition(1 + i, new Vector3(p.x, p.y, 0f));
        }
        line.SetPosition(segments + 2, new Vector3(origin.x, origin.y, 0f));
    }

    private Vector2 GetPlayerPos()
    {
        return player != null ? (Vector2)player.position : Vector2.zero;
    }

    // -------- Ability -> preview detection with overrides --------
    private void ResolvePreviewFromAbility(Ability a)
    {
        // defaults
        type = PreviewType.CircleOnMouse;
        range = 6f;
        aoeRadius = 2f;
        coneAngle = 60f;

        if (a == null)
            return;

        // 1) Shape: use the Ability’s explicit choice if not Auto
        // (SmartcastMode enum lives in Ability.cs)
        switch (a.previewMode)
        {
            case SmartcastMode.LineFromPlayer: type = PreviewType.LineFromPlayer; break;
            case SmartcastMode.CircleOnPlayer: type = PreviewType.CircleOnPlayer; break;
            case SmartcastMode.CircleOnMouse: type = PreviewType.CircleOnMouse; break;
            case SmartcastMode.ConeFromPlayer: type = PreviewType.ConeFromPlayer; break;
            case SmartcastMode.Auto:
                // leave for heuristics below
                break;
        }

        // 2) Numbers: try to read from the prefab’s scripts
        //    We don’t instantiate; we just read serialized fields on the prefab asset.
        if (a.effectPrefab != null)
        {
            // Range candidates (line / player-circle / cone)
            float r = ReadFloatFromPrefab(a.effectPrefab,
                "range", "maxRange", "projectileRange", "chainRange", "reach", "castRange", "attackRange", "seekRange");
            if (r > 0f) range = r;

            // AOE radius candidates (mouse-circle)
            float rad = ReadFloatFromPrefab(a.effectPrefab,
                "aoeRadius", "radius", "areaRadius", "groundRadius", "blastRadius", "effectRadius");
            if (rad > 0f) aoeRadius = rad;

            // Cone angle
            float ang = ReadFloatFromPrefab(a.effectPrefab, "coneAngle", "arcAngle", "spreadAngle");
            if (ang > 0f) coneAngle = Mathf.Clamp(ang, 5f, 179f);
        }

        // 3) If shape is Auto, pick based on fields & name
        if (a.previewMode == SmartcastMode.Auto)
        {
            string n = a.name ?? "";
            bool hasAoe = (aoeRadius > 0f);
            bool hasProjish = ReadHasAnyField(a.effectPrefab, "projectileSpeed", "speed", "projectilePrefab");
            bool looksChain = n.IndexOf("chain", StringComparison.OrdinalIgnoreCase) >= 0 ||
                              n.IndexOf("lightning", StringComparison.OrdinalIgnoreCase) >= 0;

            if (hasAoe) type = PreviewType.CircleOnMouse;
            else if (looksChain) type = PreviewType.CircleOnPlayer;
            else if (hasProjish) type = PreviewType.LineFromPlayer;
            else type = PreviewType.CircleOnMouse;
        }
    }

    // Helpers: read from any MonoBehaviour on the prefab (including children)
    private static float ReadFloatFromPrefab(GameObject prefab, params string[] fieldNames)
    {
        if (prefab == null) return -1f;
        var comps = prefab.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var c in comps)
        {
            var t = c.GetType();
            foreach (var name in fieldNames)
            {
                var f = t.GetField(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (f != null)
                {
                    var v = f.GetValue(c);
                    if (v is float ff && ff > 0f) return ff;
                    if (v is int ii && ii > 0) return ii;
                    if (float.TryParse(v?.ToString(), out var x) && x > 0f) return x;
                }
                var p = t.GetProperty(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (p != null && p.CanRead)
                {
                    var v = p.GetValue(c, null);
                    if (v is float fpf && fpf > 0f) return fpf;
                    if (v is int ipf && ipf > 0) return ipf;
                    if (float.TryParse(v?.ToString(), out var y) && y > 0f) return y;
                }
            }
        }
        return -1f;
    }

    private static bool ReadHasAnyField(GameObject prefab, params string[] fieldNames)
    {
        if (prefab == null) return false;
        var comps = prefab.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var c in comps)
        {
            var t = c.GetType();
            foreach (var name in fieldNames)
            {
                if (t.GetField(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance) != null)
                    return true;
                if (t.GetProperty(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance) != null)
                    return true;
            }
        }
        return false;
    }


    private bool TryApplyOverride(Ability a)
    {
        if (overrides == null || overrides.Length == 0) return false;

        string id = GetAbilityId(a);
        string name = CleanName(a.name);

        for (int i = 0; i < overrides.Length; i++)
        {
            var ov = overrides[i];
            if (string.IsNullOrWhiteSpace(ov.idOrName)) continue;

            string key = ov.idOrName.Trim();
            bool match =
                (!string.IsNullOrEmpty(id) && string.Equals(Norm(id), Norm(key), StringComparison.OrdinalIgnoreCase)) ||
                (IndexOf(name, key) >= 0);

            if (!match) continue;

            type = ov.type;
            if (ov.setRange) range = ov.range;
            if (ov.setAoeRadius) aoeRadius = ov.aoeRadius;
            if (ov.setConeAngle) coneAngle = ov.coneAngle;
            return true;
        }
        return false;
    }

    // -------- Reflection helpers --------
    private static string GetAbilityId(Ability a)
    {
        var t = a.GetType();
        var f = t.GetField("internalID", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
             ?? t.GetField("abilityId", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
             ?? t.GetField("id", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (f != null)
        {
            var v = f.GetValue(a);
            if (v != null) return v.ToString();
        }
        var p = t.GetProperty("InternalID", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
             ?? t.GetProperty("AbilityId", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
             ?? t.GetProperty("Id", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (p != null)
        {
            var v = p.GetValue(a, null);
            if (v != null) return v.ToString();
        }
        return null;
    }

    private static string GetStringFieldOrProp(object obj, string name)
    {
        var t = obj.GetType();
        var f = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (f != null) { var v = f.GetValue(obj); return v != null ? v.ToString() : null; }
        var p = t.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (p != null) { var v = p.GetValue(obj, null); return v != null ? v.ToString() : null; }
        return null;
    }

    private static bool HasAnyField(object obj, params string[] names)
    {
        var t = obj.GetType();
        for (int i = 0; i < names.Length; i++)
        {
            if (t.GetField(names[i], BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) != null) return true;
            if (t.GetProperty(names[i], BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) != null) return true;
        }
        return false;
    }

    private static float GetFirstFloat(object obj, float def, params string[] names)
    {
        var t = obj.GetType();
        for (int i = 0; i < names.Length; i++)
        {
            var f = t.GetField(names[i], BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (f != null) return ToFloat(f.GetValue(obj), def);

            var p = t.GetProperty(names[i], BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (p != null) return ToFloat(p.GetValue(obj, null), def);
        }
        return def;
    }

    private static float ToFloat(object v, float def)
    {
        if (v == null) return def;
        if (v is float f) return f;
        if (v is int i) return i;
        if (float.TryParse(v.ToString(), out var x)) return x;
        return def;
    }

    private static int IndexOf(string haystack, string needle)
    {
        return haystack != null ? haystack.IndexOf(needle, StringComparison.OrdinalIgnoreCase) : -1;
    }

    private static string CleanName(string n)
    {
        if (string.IsNullOrEmpty(n)) return "";
        const string clone = "(Clone)";
        if (n.EndsWith(clone, StringComparison.Ordinal)) return n.Substring(0, n.Length - clone.Length).Trim();
        return n;
    }

    private static string Norm(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        s = s.Trim().ToLowerInvariant();
        s = s.Replace(" ", "").Replace("_", "").Replace("-", "");
        return s;
    }
}
