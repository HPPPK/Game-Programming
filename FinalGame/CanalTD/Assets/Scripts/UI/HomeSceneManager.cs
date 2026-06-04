/*
 * File: HomeSceneManager.cs
 *
 * Purpose:
 * Controls the HomeScene menu for CanalTD. The home screen sends New Game to
 * ModeSelectScene, opens simple overlay panels, and handles quitting.
 */
using UnityEngine;
using UnityEngine.SceneManagement;

public class HomeSceneManager : MonoBehaviour
{
    [Header("Scenes")]
    public string modeSelectSceneName = "ModeSelectScene";
    public string guideSceneName = "GuideScene";

    [Header("Panels")]
    public GameObject instructionPanel;
    public GameObject settingPanel;

    private void Start()
    {
        HidePanelOnStart(instructionPanel, "Instruction panel");
        HidePanelOnStart(settingPanel, "Setting panel");
    }

    // Loads the scene where the player chooses local, AI, or future online setup.
    public void OpenModeSelectScene()
    {
        SceneManager.LoadScene(modeSelectSceneName);
    }

    // Loads the interactive tutorial scene from the Home menu Guide button.
    public void OpenGuide()
    {
        SceneManager.LoadScene(guideSceneName);
    }

    // Opens the instruction panel and closes the setting panel so only one popup is visible.
    public void OpenInstruction()
    {
        if (instructionPanel == null)
        {
            Debug.LogWarning("Instruction panel is not assigned.");
            return;
        }

        instructionPanel.SetActive(true);
        SetPanelActive(settingPanel, false, "Setting panel");
    }

    // Closes the instruction panel without changing the rest of the menu.
    public void CloseInstruction()
    {
        if (instructionPanel == null)
        {
            Debug.LogWarning("Instruction panel is not assigned.");
            return;
        }

        instructionPanel.SetActive(false);
    }

    // Opens the setting panel and closes the instruction panel so panels do not overlap.
    public void OpenSetting()
    {
        if (settingPanel == null)
        {
            Debug.LogWarning("Setting panel is not assigned.");
            return;
        }

        settingPanel.SetActive(true);
        SetPanelActive(instructionPanel, false, "Instruction panel");
    }

    // Closes the setting panel without changing the rest of the menu.
    public void CloseSetting()
    {
        if (settingPanel == null)
        {
            Debug.LogWarning("Setting panel is not assigned.");
            return;
        }

        settingPanel.SetActive(false);
    }

    // Quits the game. In the Unity Editor this only logs a message.
    public void QuitGame()
    {
        Debug.Log("QuitGame called. Application will quit in a build.");
        Application.Quit();
    }

    // Kept for older button references; New Game now opens mode selection.
    public void StartNewGame()
    {
        OpenModeSelectScene();
    }

    // Kept for older button references that used the previous How To Play method name.
    public void OpenHowToPlay()
    {
        OpenInstruction();
    }

    // Kept for older button references that used the previous How To Play method name.
    public void CloseHowToPlay()
    {
        CloseInstruction();
    }

    // Hides an optional panel when the scene starts and warns if it is missing.
    private void HidePanelOnStart(GameObject panel, string panelName)
    {
        if (panel == null)
        {
            Debug.LogWarning(panelName + " is not assigned.");
            return;
        }

        panel.SetActive(false);
    }

    // Safely changes panel visibility from public button methods.
    private void SetPanelActive(GameObject panel, bool active, string panelName)
    {
        if (panel == null)
        {
            Debug.LogWarning(panelName + " is not assigned.");
            return;
        }

        panel.SetActive(active);
    }
}
