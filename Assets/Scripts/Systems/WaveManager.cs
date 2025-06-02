using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance;

    public int currentWave = 0;
    public bool waveActive = false;
    private bool waitingForCardChoice = false;
    private int enemiesAlive = 0;

    // 🔹 Tilføjet dette flag
    private bool hasWaveStarted = false;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        StartCoroutine(DelayedStart(1f));
    }

    private IEnumerator DelayedStart(float delay)
    {
        yield return new WaitForSeconds(delay);
        StartNextWave();
    }

    private void Update()
    {
        // 🔹 Nu tjekker vi også om en wave overhovedet er startet
        if (hasWaveStarted && !waveActive && !waitingForCardChoice && AllEnemiesDefeated())
        {
            ShowCardChoice();
        }
    }

    public void RegisterEnemies(int count)
    {
        enemiesAlive = count;
        waveActive = true;
        hasWaveStarted = true; // 🔹 Markér at vi nu officielt er i gang
    }

    public void EnemyDied()
    {
        enemiesAlive--;

        if (enemiesAlive <= 0)
        {
            waveActive = false;
        }
    }

    private bool AllEnemiesDefeated()
    {
        return enemiesAlive <= 0;
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

        int oneHitCount = Mathf.Clamp(3 + currentWave, 3, 20);
        int twoHitCount = currentWave / 3;

        EnemySpawner.Instance.SpawnWave(currentWave, oneHitCount, twoHitCount);
    }
}
