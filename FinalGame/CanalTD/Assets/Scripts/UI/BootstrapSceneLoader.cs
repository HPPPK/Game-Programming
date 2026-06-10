/*
 * File: BootstrapSceneLoader.cs
 *
 * Purpose:
 * Implements BootstrapSceneLoader for the ui layer of CanalTD and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * Canvas objects, scene UI roots, status panels, buttons, or cursor feedback objects.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for BootstrapSceneLoader within the ui system.
 * - Update the owning object state and react to gameplay events during play.
 * - Present readable feedback so players can understand turns, actions, and results.
 *
 * Inputs:
 * - Inspector references configured in Unity.
 * - Runtime state from connected managers, scene objects, or event callbacks.
 * - Player input, button clicks, pointer events, or scene transition requests.
 *
 * Outputs or effects:
 * - Changes scene state, gameplay data, or visual feedback in the active match.
 * - Updates visible UI, indicators, prompts, and player-facing status messages.
 *
 * Authorship or assistance:
 * - Core gameplay design, Unity setup, and project integration were developed by Jingyu Pan.
 * - This documentation header was expanded with AI assistance to match the assessment comment standard.
 *
 * Testing notes:
 * - Verify BootstrapSceneLoader in the scene or prefab where it is used and confirm the main happy path still works.
 * - Confirm the related UI remains readable in both the normal scene flow and edge/error states.
 */
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BootstrapSceneLoader : MonoBehaviour
{
    [Header("Scene")]
    public string homeSceneName = "HomeScene";

    [Header("Loading")]
    public bool loadOnStart = true;

    /// <summary>
    /// Sets up bootstrap scene loader when this scene object starts running.
    /// </summary>
    private void Start()
    {
        if (loadOnStart)
        {
            StartCoroutine(LoadHomeAfterGlobalUIInit());
        }
    }

    // Waits one frame so Awake/Start on GlobalUIRoot can run before changing scenes.
    /// <summary>
    /// Loads home after global UI init from saved settings, room data, or scene references.
    /// </summary>
    private IEnumerator LoadHomeAfterGlobalUIInit()
    {
        yield return null;
        SceneManager.LoadScene(homeSceneName);
    }
}
