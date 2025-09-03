using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DarkRPGUI
{
    // This class manages the game panels in a Dark RPG UI environment.
    public class mr_GM : MonoBehaviour
    {
        private int index; // Index to keep track of the current panel.
        private GameObject currentPanel; // Reference to the currently active panel.

        [SerializeField]
        private GameObject[] panels; // Array to hold all the panels.

        // Called when the script instance is being loaded.
        void Start()
        {
            SetActivePanel(0); // Initialize the first panel as active.
        }

        // Method to set the next panel as active.
        public void NextPanel()
        {
            // Increment the index and loop back if it exceeds the array length.
            SetActivePanel((int)Mathf.Repeat(++index, panels.Length));
        }

        // Method to set the previous panel as active.
        public void PrevPanel()
        {
            // Decrement the index and loop back if it falls below 0.
            SetActivePanel((int)Mathf.Repeat(--index, panels.Length));
        }

        // Private method to activate a panel based on the given index.
        private void SetActivePanel(int index)
        {
            // If there's a currently active panel, deactivate it.
            if (currentPanel != null)
                currentPanel.SetActive(false);

            // Set the current panel to the new panel and activate it.
            currentPanel = panels[index];
            currentPanel.SetActive(true);
        }

        // Update is called once per frame.
        private void Update()
        {
            // Check for space key input to switch to the next panel.
            if (Input.GetKeyDown(KeyCode.Space))
            {
                NextPanel();
            }
        }
    }
}