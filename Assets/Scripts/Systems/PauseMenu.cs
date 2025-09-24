using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject pauseMenuUI; // assign PauseMenu panel
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject optionsMenu;

    private bool isPaused = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused && optionsMenu.activeSelf)
                CloseOptions();
            else if (isPaused) Resume();
            else Pause();
        }
    }

    public void Pause()
    {
        pauseMenuUI.SetActive(true);
        PauseManager.RequestPause();
        isPaused = true;

        var dimmer = FindAnyObjectByType<Dimmer>();
        if (dimmer) dimmer.Show();
    }
    
    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        optionsMenu.SetActive(false);
        PauseManager.ReleasePause();
        isPaused = false;

        var dimmer = FindAnyObjectByType<Dimmer>();
        if (dimmer) dimmer.Hide();
    }

    public void OpenOptions()
    {
        pauseMenu.SetActive(false);
        optionsMenu.SetActive(true);
    }

    public void CloseOptions()
    {
        optionsMenu.SetActive(false);
        pauseMenu.SetActive(true);
    }

    public void ExitToMainMenu()
    {
        PauseManager.ReleasePause();

        var dimmer = FindAnyObjectByType<Dimmer>();
        if (dimmer) dimmer.Hide();

        SceneManager.LoadScene("MainMenu"); // <-- replace with your main menu scene name
    }
}
