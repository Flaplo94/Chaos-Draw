using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class WalletUI : MonoBehaviour
{
    [Header("Refs")]
    public TMP_Text amountText;
    public Image icon; // valgfri

    void OnEnable()
    {
        if (Wallet.Instance != null)
            Wallet.Instance.OnGoldChanged += UpdateUI;

        UpdateUI(Wallet.Instance != null ? Wallet.Instance.Gold : 0);
    }

    void OnDisable()
    {
        if (Wallet.Instance != null)
            Wallet.Instance.OnGoldChanged -= UpdateUI;
    }

    void UpdateUI(int value)
    {
        if (amountText != null)
            amountText.text = value.ToString();
    }
}
