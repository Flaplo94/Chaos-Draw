using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject pauseMenuUI; // assign PauseMenu panel

    private bool isPaused = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) Resume();
            else Pause();
        }
    }

    public void Pause()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;
    }

    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;
    }

    public void OpenOptions()
    {
        Debug.Log("TODO: Show options menu");
        // Replace with your options UI when ready
    }

    public void ExitToMainMenu()
    {
        Debug.Log("TODO: Show Main menu");
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu"); // <-- replace with your main menu scene name
    }
}
