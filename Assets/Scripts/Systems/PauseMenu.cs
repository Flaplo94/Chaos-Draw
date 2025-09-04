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
        PauseManager.RequestPause();
        isPaused = true;
    }

    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        PauseManager.ReleasePause();
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
        PauseManager.ReleasePause();
        SceneManager.LoadScene("MainMenu"); // <-- replace with your main menu scene name
    }
}
