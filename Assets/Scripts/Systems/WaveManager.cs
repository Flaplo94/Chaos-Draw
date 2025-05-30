using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    public GameObject enemyType1; // 1-hit fjende
    public GameObject enemyType2; // 2-hit fjende
    public Transform[] spawnPoints;
    public int enemiesPerWave = 5;
    public float timeBetweenSpawns = 0.5f;

    public CardChoiceUI cardChoiceUI;

    private int currentWave = 0;
    private int enemiesAlive = 0;
    private bool isSpawning = false;

    void Start()
    {
        StartCoroutine(SpawnWave());
    }

    void Update()
    {
        if (!isSpawning && enemiesAlive == 0)
        {
            currentWave++;
            Time.timeScale = 0f;

            CardType type = (currentWave % 3 == 0) ? CardType.Ability : CardType.Buff;
            var randomCards = CardManager.Instance.GetRandomCards(3, type);
            cardChoiceUI.ShowCards(randomCards, OnCardChosen);
        }
    }

    IEnumerator SpawnWave()
    {
        isSpawning = true;

        for (int i = 0; i < enemiesPerWave; i++)
        {
            GameObject prefabToSpawn = (Random.value > 0.5f) ? enemyType1 : enemyType2;
            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

            Instantiate(prefabToSpawn, spawnPoint.position, Quaternion.identity);
            enemiesAlive++;
            yield return new WaitForSeconds(timeBetweenSpawns);
        }

        isSpawning = false;
    }

    public void EnemyDied()
    {
        enemiesAlive--;
    }

    void OnCardChosen(CardData chosen)
    {
        Debug.Log("Chosen card: " + chosen.cardName);

        if (chosen.cardEffectPrefab != null)
        {
            Instantiate(chosen.cardEffectPrefab);
        }

        ResumeGame();
    }

    void ResumeGame()
    {
        Time.timeScale = 1f;
        StartCoroutine(SpawnWave());
    }
}
