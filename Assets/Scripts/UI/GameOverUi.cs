using UnityEngine;

public class GameOverUI : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public GameObject restartButton;
    public float fadeDuration = 1.5f;

    private void Start()
    {
        canvasGroup.alpha = 0f;

        // Sørg for at knappen er skjult fra start
        if (restartButton != null)
        {
            restartButton.SetActive(false);
        }
    }

    public void ShowGameOver()
    {
        StartCoroutine(FadeIn());
    }

    private System.Collections.IEnumerator FadeIn()
    {
        float timer = 0f;

        while (timer < fadeDuration)
        {
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
            timer += Time.deltaTime;
            yield return null;
        }

        canvasGroup.alpha = 1f;

        if (restartButton != null)
        {
            restartButton.SetActive(true); // Gør knappen synlig
        }
    }
}
