using UnityEngine;

public class MenuManager : MonoBehaviour
{
    [Header("Main Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Settings Sub-Panels")]
    [SerializeField] private GameObject audioPanel;
    [SerializeField] private GameObject displayPanel;
    [SerializeField] private GameObject controlsPanel;

    void Start()
    {
        ShowMainMenu();
    }

    // --- Main Menu ---
    public void ShowMainMenu()
    {
        mainMenuPanel.SetActive(true);
        settingsPanel.SetActive(false);
    }

    // --- Settings Menu ---
    public void ShowSettingsMenu()
    {
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(true);
        HideAllSubPanels();
    }

    public void ShowAudioPanel()
    {
        HideAllSubPanels();
        audioPanel.SetActive(true);
    }

    public void ShowDisplayPanel()
    {
        HideAllSubPanels();
        displayPanel.SetActive(true);
    }

    public void ShowControlsPanel()
    {
        HideAllSubPanels();
        controlsPanel.SetActive(true);
    }

    public void BackToMainMenu()
    {
        ShowMainMenu();
    }

    private void HideAllSubPanels()
    {
        audioPanel.SetActive(false);
        displayPanel.SetActive(false);
        controlsPanel.SetActive(false);
    }
}
