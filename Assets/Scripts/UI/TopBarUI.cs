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

    void OnEnable()
    {
        // Inventory events
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnArtifactAdded += AddArtifactIcon;
            PlayerInventory.Instance.OnBuffAdded += AddBuffIcon;

            // initial icons
            foreach (var art in PlayerInventory.Instance.artifacts)
                AddArtifactIcon(art);
            foreach (var buff in PlayerInventory.Instance.buffs)
                AddBuffIcon(buff);
        }

        // Wallet events
        if (Wallet.Instance != null)
        {
            Wallet.Instance.OnGoldChanged += UpdateGold;
            UpdateGold(Wallet.Instance.CurrentGold); // init
        }

        // Wave events
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnWaveChanged += UpdateWave;
            UpdateWave(WaveManager.Instance.CurrentWave); // init
        }
    }

    void OnDisable()
    {
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnArtifactAdded -= AddArtifactIcon;
            PlayerInventory.Instance.OnBuffAdded -= AddBuffIcon;
        }

        if (Wallet.Instance != null)
            Wallet.Instance.OnGoldChanged -= UpdateGold;

        if (WaveManager.Instance != null)
            WaveManager.Instance.OnWaveChanged -= UpdateWave;
    }

    private void AddArtifactIcon(ArtifactData artifact)
    {
        if (!artifact || !iconPrefab || !artifactsContent) return;
        GameObject go = Instantiate(iconPrefab, artifactsContent);
        var img = go.GetComponent<Image>();
        if (img) img.sprite = artifact.icon;
    }

    private void AddBuffIcon(BuffData buff)
    {
        if (!buff || !iconPrefab || !buffsContent) return;
        GameObject go = Instantiate(iconPrefab, buffsContent);
        var img = go.GetComponent<Image>();
        if (img) img.sprite = buff.icon;
    }

    private void UpdateGold(int amount)
    {
        if (goldText != null)
            goldText.text = amount.ToString();
    }

    private void UpdateWave(int wave)
    {
        if (waveText != null)
            waveText.text = "Wave " + wave;
    }
}
