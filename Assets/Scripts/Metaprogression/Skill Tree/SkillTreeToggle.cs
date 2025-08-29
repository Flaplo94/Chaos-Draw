using UnityEngine;

public class SkillTreeToggle : MonoBehaviour
{
    public GameObject mainMenuGroup;
    public GameObject skillTreeGroup;

    void Start()
    {
        // Start i menuen
        mainMenuGroup.SetActive(true);
        skillTreeGroup.SetActive(false);
    }

    public void OpenSkillTree()
    {
        mainMenuGroup.SetActive(false);
        skillTreeGroup.SetActive(true);
    }

    public void CloseSkillTree()
    {
        skillTreeGroup.SetActive(false);
        mainMenuGroup.SetActive(true);
    }
}
