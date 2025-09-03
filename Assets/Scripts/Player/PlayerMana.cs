using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerMana : MonoBehaviour
{
    public static PlayerMana Instance { get; private set; }

    [Header("Mana Settings")]
    public float maxMana = 100f;
    public float regenRate = 5f;
    public float currentMana;

    [Header("UI References")]
    [SerializeField] private Image manaFill;    // Blå fyld
    [SerializeField] private Image manaEffect;  // Effekt ovenpå fyld
    [SerializeField] private TMP_Text manaText; // Tekst current/max
    [SerializeField] private RectTransform edgeVfx; // Lys-streg

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        currentMana = maxMana;
        UpdateUI();
    }

    void Update()
    {
        if (currentMana < maxMana)
        {
            currentMana += regenRate * Time.deltaTime;
            if (currentMana > maxMana)
                currentMana = maxMana;

            UpdateUI();
        }
    }

    public bool TrySpend(float amount)
    {
        if (currentMana >= amount)
        {
            currentMana -= amount;
            UpdateUI();
            Debug.Log($"[Mana] Spent {amount}, now {currentMana:F2}");
            return true;
        }

        Debug.Log("[Mana] Not enough mana!");
        return false;
    }

    private void UpdateUI()
    {
        float ratio = currentMana / maxMana;

        if (manaFill != null)
            manaFill.fillAmount = ratio;

        if (manaEffect != null)
            manaEffect.fillAmount = ratio;

        if (manaText != null)
            manaText.text = $"{Mathf.FloorToInt(currentMana)}/{Mathf.FloorToInt(maxMana)}";

        if (edgeVfx != null && manaFill != null)
        {
            float height = ((RectTransform)manaFill.transform).rect.height;
            edgeVfx.anchoredPosition = new Vector2(
                edgeVfx.anchoredPosition.x,
                -height / 2f + height * ratio
            );
        }
    }
}
