#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class CreateStatBox2Col
{
    [MenuItem("GameObject/ChaosDraw/Create StatBox (2-Column)", false, 10)]
    public static void Create()
    {
        var parent = Selection.activeTransform;

        // --- StatBox root ---
        var statBox = new GameObject("StatBox", typeof(RectTransform), typeof(Image));
        if (parent) statBox.transform.SetParent(parent, false);
        var sbRT = statBox.GetComponent<RectTransform>();
        sbRT.anchorMin = new Vector2(0, 1);      // top-left
        sbRT.anchorMax = new Vector2(0, 1);
        sbRT.pivot = new Vector2(0, 1);
        sbRT.anchoredPosition = Vector2.zero;
        sbRT.sizeDelta = new Vector2(327.5f, 156.5f);

        // --- Header ---
        var header = new GameObject("Header", typeof(RectTransform), typeof(TextMeshProUGUI));
        header.transform.SetParent(statBox.transform, false);
        var hRT = header.GetComponent<RectTransform>();
        hRT.anchorMin = new Vector2(0, 1); hRT.anchorMax = new Vector2(1, 1); hRT.pivot = new Vector2(0.5f, 1);
        hRT.sizeDelta = new Vector2(0, 22); hRT.anchoredPosition = new Vector2(0, -8);
        var hTxt = header.GetComponent<TextMeshProUGUI>();
        hTxt.text = "Stats"; hTxt.alignment = TextAlignmentOptions.TopLeft; hTxt.fontSize = 20;

        // --- Content (Horizontal) ---
        var content = new GameObject("Content", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        content.transform.SetParent(statBox.transform, false);
        var cRT = content.GetComponent<RectTransform>();
        cRT.anchorMin = new Vector2(0, 0); cRT.anchorMax = new Vector2(1, 1); cRT.pivot = new Vector2(0.5f, 0.5f);
        // Offsets: L=10, R=10, Top=36, Bottom=8
        cRT.offsetMin = new Vector2(10, 8);      // left,bottom
        cRT.offsetMax = new Vector2(-10, -36);   // -right, -top
        var hlg = content.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 12; hlg.childAlignment = TextAnchor.UpperLeft;
        hlg.childControlWidth = true; hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = false;

        // --- ColLeft ---
        var colLeft = new GameObject("ColLeft", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        colLeft.transform.SetParent(content.transform, false);
        var vlL = colLeft.GetComponent<VerticalLayoutGroup>();
        vlL.spacing = 6; vlL.childAlignment = TextAnchor.UpperLeft;
        vlL.childControlWidth = true; vlL.childControlHeight = true;
        vlL.childForceExpandWidth = true; vlL.childForceExpandHeight = false;
        colLeft.GetComponent<LayoutElement>().flexibleWidth = 1;

        // --- ColRight ---
        var colRight = new GameObject("ColRight", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        colRight.transform.SetParent(content.transform, false);
        var vlR = colRight.GetComponent<VerticalLayoutGroup>();
        vlR.spacing = 6; vlR.childAlignment = TextAnchor.UpperLeft;
        vlR.childControlWidth = true; vlR.childControlHeight = true;
        vlR.childForceExpandWidth = true; vlR.childForceExpandHeight = false;
        colRight.GetComponent<LayoutElement>().flexibleWidth = 1;

        // Helper til at lave en StatRow instance (ikon+spacer+value)
        System.Action<Transform, string> addRow = (col, name) =>
        {
            var row = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement), typeof(Image));
            row.transform.SetParent(col, false);
            var rowRT = row.GetComponent<RectTransform>();
            rowRT.anchorMin = new Vector2(0, 0.5f); rowRT.anchorMax = new Vector2(1, 0.5f);
            rowRT.pivot = new Vector2(0.5f, 0.5f); rowRT.sizeDelta = new Vector2(0, 22);
            var rowHLG = row.GetComponent<HorizontalLayoutGroup>();
            rowHLG.spacing = 6; rowHLG.childAlignment = TextAnchor.MiddleLeft;
            rowHLG.childControlWidth = true; rowHLG.childControlHeight = true;
            rowHLG.childForceExpandWidth = true; rowHLG.childForceExpandHeight = false;
            row.GetComponent<LayoutElement>().minHeight = 22;
            // svag baggrund for visual debug
            var bg = row.GetComponent<Image>(); bg.color = new Color(1, 1, 1, 0.03f);

            // Icon
            var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            icon.transform.SetParent(row.transform, false);
            var iconLE = icon.GetComponent<LayoutElement>(); iconLE.preferredWidth = 20; iconLE.preferredHeight = 20;
            icon.GetComponent<Image>().preserveAspect = true;

            // Spacer
            var spacer = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
            spacer.transform.SetParent(row.transform, false);
            spacer.GetComponent<LayoutElement>().flexibleWidth = 1;

            // Value
            var value = new GameObject("Value", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            value.transform.SetParent(row.transform, false);
            var valLE = value.GetComponent<LayoutElement>(); valLE.preferredWidth = 96;
            var valTMP = value.GetComponent<TextMeshProUGUI>();
            valTMP.alignment = TextAlignmentOptions.MidlineRight; valTMP.fontSize = 16;
            valTMP.text = "×1.00  (+0%)";
        };

        // 3 + 3 demo-rækker, så layoutet er synligt
        addRow(colLeft.transform, "Row_BasicDamage");
        addRow(colLeft.transform, "Row_AttackSpeed");
        addRow(colLeft.transform, "Row_MoveSpeed");
        addRow(colRight.transform, "Row_FireDamage");
        addRow(colRight.transform, "Row_LightningDamage");
        addRow(colRight.transform, "Row_BurnDamage");

        Selection.activeGameObject = statBox;
        Debug.Log("StatBox (2-Column) created. Drag en Row ud i Project for at lave et StatRow prefab asset.");
    }
}
#endif
