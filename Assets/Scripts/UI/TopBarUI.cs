using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TopBarUI : MonoBehaviour
{
    [Header("Parents")]
    public Transform artifactsContent;
    public Transform buffsContent;

    [Header("Prefabs")]
    public GameObject iconPrefab;

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI waveText;

    // --- Added: robust wallet hook state
    private Wallet _subscribedWallet;

    void OnEnable()
    {
        // Inventory events
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnArtifactAdded += AddArtifactIcon;
            PlayerInventory.Instance.OnItemAdded += AddItemIcon;

            // initial icons
            foreach (var art in PlayerInventory.Instance.artifacts)
                AddArtifactIcon(art);
            foreach (var buff in PlayerInventory.Instance.items)
                AddItemIcon(buff);
        }

        // Wallet events (robust to load order)
        TryHookWallet();

        // Wave events
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnWaveChanged += UpdateWave;
            UpdateWave(WaveManager.Instance.CurrentWave); // init
        }
    }

    void Update()
    {
        // In case Wallet appears later (scene order), this will hook once and no-op afterwards.
        TryHookWallet();
    }

    void OnDisable()
    {
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnArtifactAdded -= AddArtifactIcon;
            PlayerInventory.Instance.OnItemAdded -= AddItemIcon;
        }

        // Unhook wallet safely if we were subscribed
        if (_subscribedWallet != null)
        {
            _subscribedWallet.OnGoldChanged -= UpdateGold;
            _subscribedWallet = null;
        }

        if (WaveManager.Instance != null)
            WaveManager.Instance.OnWaveChanged -= UpdateWave;
    }

    // --- Added: late-binding wallet subscribe helper
    private void TryHookWallet()
    {
        var w = Wallet.Instance;
        if (w == null || w == _subscribedWallet) return;

        // unhook previous (should be null normally)
        if (_subscribedWallet != null)
            _subscribedWallet.OnGoldChanged -= UpdateGold;

        _subscribedWallet = w;
        _subscribedWallet.OnGoldChanged += UpdateGold;
        UpdateGold(_subscribedWallet.CurrentGold); // initial value
    }

    private void AddArtifactIcon(ArtifactData artifact)
    {
        if (!artifact || !iconPrefab || !artifactsContent) return;
        GameObject go = Instantiate(iconPrefab, artifactsContent);
        var img = go.GetComponent<Image>();
        if (img) img.sprite = artifact.icon;
    }

    private void AddItemIcon(ItemData item)
    {
        if (!item || !iconPrefab || !buffsContent) return;
        GameObject go = Instantiate(iconPrefab, buffsContent);
        var img = go.GetComponent<Image>();
        if (img) img.sprite = item.icon;
    }

    // --- Optional: warn once if goldText is not assigned
    private bool _warnedGoldText;
    private void UpdateGold(int amount)
    {
        if (goldText != null)
        {
            goldText.text = amount.ToString();
        }
        else if (!_warnedGoldText)
        {
            _warnedGoldText = true;
            Debug.LogWarning("[TopBarUI] goldText is not assigned.");
        }
    }

    private void UpdateWave(int wave)
    {
        if (waveText != null)
            waveText.text = "WAVE " + wave;
    }
}
