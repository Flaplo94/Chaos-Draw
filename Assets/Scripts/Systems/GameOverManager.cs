using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class GameOverManager : MonoBehaviour
{
    public GameObject gameOverUI;
    public float delayBeforeGameOverUI = 0f;
    public GameObject waveCounterUI;
    public GameObject healthBarUI;

    public void TriggerGameOver()
    {
        waveCounterUI?.SetActive(false);
        healthBarUI?.SetActive(false);
        gameOverUI.SetActive(true);
    }

    public void Retry()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}