using UnityEngine;
using UnityEngine.UI;

public class PlayerMana : MonoBehaviour
{
    [Header("Mana Settings")]
    [SerializeField] private float maxMana = 100f;
    [SerializeField] private float regenRate = 5f;
    [SerializeField] private Slider manaBar;

    private float currentMana;

    public static PlayerMana Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        currentMana = maxMana;
        UpdateUI();
    }

    private void Update()
    {
        if (currentMana < maxMana)
        {
            currentMana += regenRate * Time.deltaTime;
            currentMana = Mathf.Min(currentMana, maxMana);
            UpdateUI();
        }
    }

    public bool TrySpend(float amount)
    {
        if (currentMana < amount)
            return false;

        currentMana -= amount;
        UpdateUI();
        return true;
    }

    private void UpdateUI()
    {
        if (manaBar != null)
        {
            manaBar.value = currentMana / maxMana;
        }
    }
}
