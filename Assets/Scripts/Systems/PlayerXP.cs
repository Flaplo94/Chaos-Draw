using UnityEngine;

public class PlayerXP : MonoBehaviour
{
    public static PlayerXP Instance;

    public int currentXP = 0;
    public int xpToLevelUp = 5;
    public int level = 1;

    private void Awake()
    {
        // Singleton - gør det let at få adgang til fra andre scripts
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void GainXP(int amount)
    {
        currentXP += amount;
        Debug.Log("XP: " + currentXP + "/" + xpToLevelUp);

        if (currentXP >= xpToLevelUp)
        {
            currentXP = 0;
            level++;
            Debug.Log("LEVEL UP! Level: " + level);

            // Prøv at finde kort-UI'et og vis det
            //var cardUI = Object.FindFirstObjectByType<CardChoiceUI>();
            //if (cardUI != null)
            //{
            //    cardUI.ShowCardChoices();
            //}
            //else
            //{
            //    Debug.LogWarning("CardChoiceUI not found in scene!");
            //}
        }
    }
}
