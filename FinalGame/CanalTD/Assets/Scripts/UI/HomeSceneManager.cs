/*
 * File: HomeSceneManager.cs
 *
 * Purpose:
 * Controls the simple HomeScene menu for CanalTD. It starts the game, opens and
 * closes the How To Play panel, and handles quitting the application.
 */
using UnityEngine;
using UnityEngine.SceneManagement;

public class HomeSceneManager : MonoBehaviour
{
    [Header("Scenes")]
    public string gameSceneName = "GameScene";

    [Header("Panels")]
    public GameObject howToPlayPanel;

    // Loads the main CanalTD gameplay scene.
    public void StartNewGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    // Shows the How To Play panel if it is assigned.
    public void OpenHowToPlay()
    {
        if (howToPlayPanel == null)
        {
            Debug.LogWarning("How To Play panel is not assigned.");
            return;
        }

        howToPlayPanel.SetActive(true);
    }

    // Hides the How To Play panel if it is assigned.
    public void CloseHowToPlay()
    {
        if (howToPlayPanel == null)
        {
            Debug.LogWarning("How To Play panel is not assigned.");
            return;
        }

        howToPlayPanel.SetActive(false);
    }

    // Quits the game. In the Unity Editor this only logs a message.
    public void QuitGame()
    {
        Debug.Log("QuitGame called. Application will quit in a build.");
        Application.Quit();
    }
}
