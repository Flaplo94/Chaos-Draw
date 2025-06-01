using UnityEngine;
using System.Collections.Generic;

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance;

    public int currentWave = 0;
    public bool waveActive = false;
    private bool waitingForCardChoice = false;
    private int enemiesAlive = 0;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Update()
    {
        if (!waveActive && !waitingForCardChoice)
        {
            ShowCardChoice();
        }
    }

    public void RegisterEnemies(int count)
    {
        enemiesAlive = count;
        waveActive = true;
    }

    public void EnemyDied()
    {
        enemiesAlive--;

        if (enemiesAlive <= 0)
        {
            waveActive = false;
        }
    }

    private void ShowCardChoice()
    {
        CardType type = currentWave % 3 == 0 ? CardType.Ability : CardType.Buff;
        List<CardData> cards = CardManager.Instance.GetRandomCards(3, type);

        var choiceUI = FindFirstObjectByType<CardChoiceUI>();
        waitingForCardChoice = true;

        choiceUI.ShowCards(cards, (selectedCard) =>
        {
            Debug.Log("Player selected: " + selectedCard.cardName);

            if (selectedCard.cardType == CardType.Buff)
                BuffSystem.Instance.AddBuff(selectedCard);
            else
                AbilitySystem.Instance.AddAbility(selectedCard);

            waitingForCardChoice = false;
            StartNextWave();
        });
    }

    private void StartNextWave()
    {
        currentWave++;
        EnemySpawner.Instance.SpawnWave(currentWave);
    }
}
