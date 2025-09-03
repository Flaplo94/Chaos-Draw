using UnityEngine;
using UnityEngine.SceneManagement; // til Quit -> MainMenu

public class PauseMenu : MonoBehaviour
{
    public static bool GameIsPaused = false;

    [Header("Refs")]
    [SerializeField] private GameObject pauseMenuUI;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (GameIsPaused)
                Resume();
            else
                Pause();
        }
    }

    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f; // spillet kører igen
        GameIsPaused = false;
    }

    public void Pause()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f; // spillet fryser
        GameIsPaused = true;
    }

    public void QuitToMenu()
    {
        Time.timeScale = 1f; // reset tidsfrys
        SceneManager.LoadScene("MainMenu"); // skift navnet hvis din menu hedder noget andet
    }

    public void OpenOptions()
    {
        Debug.Log("Options menu open – her kan vi bygge videre senere");
    }
}
