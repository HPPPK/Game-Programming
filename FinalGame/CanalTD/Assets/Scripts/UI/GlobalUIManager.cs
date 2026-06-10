/*
 * File: GlobalUIManager.cs
 *
 * Purpose:
 * Implements GlobalUIManager for the ui layer of CanalTD and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * Canvas objects, scene UI roots, status panels, buttons, or cursor feedback objects.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for GlobalUIManager within the ui system.
 * - Coordinate related objects, state changes, and cross-system communication.
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
 * - Verify GlobalUIManager in the scene or prefab where it is used and confirm the main happy path still works.
 * - Confirm the related UI remains readable in both the normal scene flow and edge/error states.
 */
using System.Collections;
using TMPro;
using UnityEngine;

public class GlobalUIManager : MonoBehaviour
{
    public static GlobalUIManager Instance { get; private set; }

    [Header("Panels")]
    public InstructionBookController instructionBook;
    public SettingsPanelController settingsPanel;

    [Header("Toast")]
    public GameObject toastRoot;
    public TMP_Text toastText;
    public float toastDuration = 2f;

    private Coroutine toastCoroutine;

    /// <summary>
    /// Finds and stores global UI manager references before scene gameplay begins.
    /// </summary>
    private void Awake()
    {
        // Prevent duplicate GlobalUIRoot objects when returning to BootstrapScene.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (transform.parent != null)
        {
            Debug.LogWarning("GlobalUIManager should be placed on a root GameObject before DontDestroyOnLoad.");
        }

        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Sets up global UI manager when this scene object starts running.
    /// </summary>
    private void Start()
    {
        CloseInstruction();
        CloseSettings();

        if (toastRoot != null)
        {
            toastRoot.SetActive(false);
        }
    }

    // Opens the instruction panel if closed, otherwise closes it.
    /// <summary>
    /// Handles toggle instruction for UI display, input, or player feedback.
    /// </summary>
    public void ToggleInstruction()
    {
        if (instructionBook == null)
        {
            Debug.LogWarning("GlobalUIManager instructionBook is not assigned.");
            return;
        }

        if (instructionBook.IsOpen())
        {
            instructionBook.CloseBook();
        }
        else
        {
            instructionBook.OpenBook();
        }
    }

    // Closes the instruction panel without affecting other UI.
    /// <summary>
    /// Handles close instruction for UI display, input, or player feedback.
    /// </summary>
    public void CloseInstruction()
    {
        if (instructionBook != null)
        {
            instructionBook.CloseBook();
        }
    }

    // Opens the settings panel if closed, otherwise closes it.
    /// <summary>
    /// Handles toggle settings for UI display, input, or player feedback.
    /// </summary>
    public void ToggleSettings()
    {
        if (settingsPanel == null)
        {
            Debug.LogWarning("GlobalUIManager settingsPanel is not assigned.");
            return;
        }

        if (settingsPanel.IsOpen())
        {
            settingsPanel.CloseSettings();
        }
        else
        {
            settingsPanel.OpenSettings();
        }
    }

    // Closes the settings panel without affecting other UI.
    /// <summary>
    /// Handles close settings for UI display, input, or player feedback.
    /// </summary>
    public void CloseSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.CloseSettings();
        }
    }

    // Shows a temporary toast message and hides it after toastDuration seconds.
    /// <summary>
    /// Shows toast with the correct current context.
    /// </summary>
    public void ShowToast(string message)
    {
        Debug.Log(message);

        if (toastText == null && toastRoot != null)
        {
            toastText = toastRoot.GetComponentInChildren<TMP_Text>(true);
        }

        if (toastRoot == null && toastText == null)
        {
            Debug.LogWarning("GlobalUIManager toast UI is not assigned.");
            return;
        }

        if (toastRoot != null)
        {
            toastRoot.SetActive(true);
        }

        if (toastText != null)
        {
            toastText.gameObject.SetActive(true);
            toastText.text = message;
        }

        if (toastCoroutine != null)
        {
            StopCoroutine(toastCoroutine);
        }

        toastCoroutine = StartCoroutine(HideToastAfterDelay());
    }

    // Coroutine used by ShowToast so repeated messages reset the hide timer.
    /// <summary>
    /// Hides toast after delay and clears temporary visual state.
    /// </summary>
    private IEnumerator HideToastAfterDelay()
    {
        /// <summary>
        /// Handles wait for seconds for global UI manager.
        /// </summary>
        yield return new WaitForSeconds(toastDuration);

        if (toastRoot != null)
        {
            toastRoot.SetActive(false);
        }
        else if (toastText != null)
        {
            toastText.gameObject.SetActive(false);
        }

        toastCoroutine = null;
    }
}
