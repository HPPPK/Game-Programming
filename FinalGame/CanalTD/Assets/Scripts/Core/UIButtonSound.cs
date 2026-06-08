/*
 * File: UIButtonSound.cs
 *
 * Purpose:
 * Implements UIButtonSound for the core layer of Rail Rumble and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * Core gameplay managers, player objects, turn systems, or shared scene controllers.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for UIButtonSound within the core system.
 * - Update the owning object state and react to gameplay events during play.
 *
 * Inputs:
 * - Inspector references configured in Unity.
 * - Runtime state from connected managers, scene objects, or event callbacks.
 *
 * Outputs or effects:
 * - Changes scene state, gameplay data, or visual feedback in the active match.
 *
 * Authorship or assistance:
 * - Core gameplay design, Unity setup, and project integration were developed by Panjingyu and teammates.
 * - This documentation header was expanded with AI assistance to match the assessment comment standard.
 *
 * Testing notes:
 * - Verify UIButtonSound in the scene or prefab where it is used and confirm the main happy path still works.
 */
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIButtonSound : MonoBehaviour
{
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();

        if (button != null)
        {
            button.onClick.AddListener(PlayClickSound);
        }
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(PlayClickSound);
        }
    }

    private void PlayClickSound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUiClick();
        }
    }
}