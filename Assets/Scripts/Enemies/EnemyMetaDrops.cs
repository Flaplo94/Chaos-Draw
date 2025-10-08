using UnityEngine;
using TMPro;

[DisallowMultipleComponent]
public class EnemyMetaDrops : MonoBehaviour
{
    [Header("Drop Amounts (inclusive ranges)")]
    [Min(0)] public int shardsMin = 1;
    [Min(0)] public int shardsMax = 3;
    [Min(0)] public int xpMin = 1;
    [Min(0)] public int xpMax = 2;

    [Header("Multipliers")]
    [Tooltip("Multiply shards by PlayerBuffManager.GetChaosShardGainMult() if available.")]
    public bool applyShardGainMultiplier = true;

    [Header("Grant Timing")]
    [Tooltip("Grant rewards when this GameObject is destroyed.")]
    public bool grantOnDestroy = true;
    [Tooltip("Skip double-grant if GrantRewards() is called manually by your death code.")]
    public bool preventDoubleGrant = true;

    [Header("Popup")]
    [Tooltip("UI/World popup prefab that has WorldPopupText on it. Can be UGUI (TextMeshProUGUI) or world TMP.")]
    public WorldPopupText popupPrefab;
    [Tooltip("Optional explicit Canvas to use for UGUI popups. If null, we auto-find one in scene.")]
    public Canvas explicitCanvas;
    [Tooltip("Offset from enemy position where the popup should appear (world units).")]
    public Vector3 popupOffset = new Vector3(0f, 0.8f, 0f);
    [Tooltip("Text color for shard line.")]
    public Color shardsColor = new Color(0.6f, 0.9f, 1f);
    [Tooltip("Text color for XP line.")]
    public Color xpColor = new Color(0.95f, 0.95f, 0.55f);

    private bool granted;
    private static bool appQuitting = false;
    void OnApplicationQuit() { appQuitting = true; }

   
    public void GrantRewards()
    {
        if (preventDoubleGrant && granted) return;
        granted = true;

        int shards = Random.Range(shardsMin, shardsMax + 1);
        int xp = Random.Range(xpMin, xpMax + 1);

        if (applyShardGainMultiplier && PlayerBuffManager.Instance != null)
        {
            try
            {
                float mult = Mathf.Max(0f, PlayerBuffManager.Instance.GetChaosShardGainMult());
                shards = Mathf.RoundToInt(shards * mult);
            }
            catch { }
        }

       
        if (MetaProgressionManager.Instance != null)
        {
            if (shards > 0) MetaProgressionManager.Instance.AddShards(shards);
            if (xp > 0) MetaProgressionManager.Instance.AddXP(xp);
        }

        ShowPopup(shards, xp);
    }

    private void OnDestroy()
    {
        
        if (!Application.isPlaying || appQuitting) return;

        if (grantOnDestroy) GrantRewards();
    }

    private void ShowPopup(int shards, int xp)
    {
        if (popupPrefab == null) return;

        
        string line1 = shards > 0 ? $"+{shards} Shards" : string.Empty;
        string line2 = xp > 0 ? $"+{xp} XP" : string.Empty;

        if (string.IsNullOrEmpty(line1) && string.IsNullOrEmpty(line2))
            return;

        string text;
        if (!string.IsNullOrEmpty(line1) && !string.IsNullOrEmpty(line2))
        {
            text =
                $"<color=#{ColorUtility.ToHtmlStringRGB(shardsColor)}>{line1}</color>\n" +
                $"<color=#{ColorUtility.ToHtmlStringRGB(xpColor)}>{line2}</color>";
        }
        else if (!string.IsNullOrEmpty(line1))
        {
            text = $"<color=#{ColorUtility.ToHtmlStringRGB(shardsColor)}>{line1}</color>";
        }
        else
        {
            text = $"<color=#{ColorUtility.ToHtmlStringRGB(xpColor)}>{line2}</color>";
        }

        Vector3 worldPos = transform.position + popupOffset;

       
        Canvas targetCanvas = explicitCanvas != null ? explicitCanvas : FindFirstObjectByType<Canvas>();

        WorldPopupText popupInstance = null;

        if (targetCanvas != null)
        {
            popupInstance = Instantiate(popupPrefab, targetCanvas.transform);
            
            Camera cam = Camera.main;
            Vector3 screenPos = cam != null ? cam.WorldToScreenPoint(worldPos) : worldPos;
            popupInstance.InitializeAtWorldPosition(worldPos, cam, screenPosOverride: screenPos);
        }
        else
        {
            popupInstance = Instantiate(popupPrefab, worldPos, Quaternion.identity);
            Camera cam = Camera.main;
            popupInstance.InitializeAtWorldPosition(worldPos, cam);
        }

        popupInstance.SetText(text);
    }
}
