using UnityEngine;
using TMPro;

public class SkillTreeUI : MonoBehaviour
{
    public static SkillTreeUI Instance;

    [Header("Refs")]
    [SerializeField] private SkillDetailPanel detailPanel;

    [Header("UI")]
    [SerializeField] private TMP_Text shardsText;  // shard counter i TabPanelet

    [Header("Spirals")]
    [SerializeField] private GameObject spiralFire;
    [SerializeField] private GameObject spiralLightning;

    [Header("Tabs")]
    [SerializeField] private GameObject fireTabIcon;
    [SerializeField] private GameObject lightningTabIcon;

    private GameObject activeSpiral;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Start altid med Fire vist
        ShowSpiral(spiralFire);

        // Tjek om Lightning allerede er unlocked fra tidligere
        bool lightningUnlocked = PlayerPrefs.GetInt("lightningUnlocked", 0) == 1;
        if (lightningUnlocked)
        {
            if (lightningTabIcon != null)
                lightningTabIcon.SetActive(true);
        }
        else
        {
            if (lightningTabIcon != null)
                lightningTabIcon.SetActive(false);
        }
    }

    void OnEnable()
    {
        MetaProgressionManager.OnShardsChanged += RefreshShardsUI;
        RefreshShardsUI(); // opdater straks når menu åbner
    }

    void OnDisable()
    {
        MetaProgressionManager.OnShardsChanged -= RefreshShardsUI;
    }

    public void OpenDetail(SkillData skill)
    {
        if (detailPanel != null)
            detailPanel.Open(skill);
    }

    public void RefreshShardsUI()
    {
        if (MetaProgressionManager.Instance != null && shardsText != null)
            shardsText.text = MetaProgressionManager.Instance.GetShards().ToString();
    }

    // === Spiral skift ===
    public void ShowFireSpiral() => ShowSpiral(spiralFire);
    public void ShowLightningSpiral() => ShowSpiral(spiralLightning);

    private void ShowSpiral(GameObject spiral)
    {
        if (activeSpiral == spiral) return;

        if (activeSpiral != null)
            activeSpiral.SetActive(false);

        spiral.SetActive(true);
        activeSpiral = spiral;
    }

    // === Unlock spiraler via skills ===
    public void UnlockSpiral(string id)
    {
        if (id == "Lightning")
        {
            if (lightningTabIcon != null)
                lightningTabIcon.SetActive(true);

            // Gem permanent
            PlayerPrefs.SetInt("lightningUnlocked", 1);
            PlayerPrefs.Save();

            Debug.Log("[SkillTreeUI] Lightning Spiral unlocked (saved).");
        }
    }
}
