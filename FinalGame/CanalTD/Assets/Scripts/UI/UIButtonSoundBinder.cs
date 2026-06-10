/*
 * File: UIButtonSoundBinder.cs
 *
 * Purpose:
 * Implements UIButtonSoundBinder for the ui layer of CanalTD and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * Canvas objects, scene UI roots, status panels, buttons, or cursor feedback objects.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for UIButtonSoundBinder within the ui system.
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
 * - Verify UIButtonSoundBinder in the scene or prefab where it is used and confirm the main happy path still works.
 * - Confirm the related UI remains readable in both the normal scene flow and edge/error states.
 */
using UnityEngine;
using UnityEngine.UI;

public class UIButtonSoundBinder : MonoBehaviour
{
    private Button[] buttons;

    /// <summary>
    /// Sets up UI button sound binder when this scene object starts running.
    /// </summary>
    private void Start()
    {
        BindAllButtons();
    }

    /// <summary>
    /// Handles bind all buttons for UI display, input, or player feedback.
    /// </summary>
    private void BindAllButtons()
    {
        buttons = GetComponentsInChildren<Button>(true);

        foreach (Button button in buttons)
        {
            if (button == null)
            {
                continue;
            }

            button.onClick.RemoveListener(PlayClickSound);
            button.onClick.AddListener(PlayClickSound);
        }
    }

    /// <summary>
    /// Removes UI button sound binder listeners and temporary references before destruction.
    /// </summary>
    private void OnDestroy()
    {
        if (buttons == null)
        {
            return;
        }

        foreach (Button button in buttons)
        {
            if (button != null)
            {
                button.onClick.RemoveListener(PlayClickSound);
            }
        }
    }

    /// <summary>
    /// Plays the click sound audio feedback.
    /// </summary>
    private void PlayClickSound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUiClick();
        }
    }
}
